using System.Collections.Generic;
using System;

namespace C7GameData

/*
  This will read a Civ3 sav into C7 native format for immediate use or saving to native JSON save
*/
{
	using QueryCiv3;
	using QueryCiv3.Biq;

	// Additional parameters used to refer to specific media files and tiles in Civ3
	public class Civ3ExtraInfo
	{
		public int BaseTerrainFileID;
		public int BaseTerrainImageID;
	}
	public class ImportCiv3
	{
		public static C7SaveFormat ImportSav(string savePath, string defaultBicPath)
		{
			// init empty C7 save
			C7SaveFormat c7Save = new C7SaveFormat();

			// Get save data reader
			byte[] defaultBicBytes = Util.ReadFile(defaultBicPath);
			SavData civ3Save = new SavData(Util.ReadFile(savePath), defaultBicBytes);
			BiqData theBiq = civ3Save.Bic;

			ImportUnitPrototypes(theBiq, c7Save);
			ImportCiv3TerrainTypes(theBiq, c7Save);
			ImportCiv3ExperienceLevels(theBiq, c7Save);
			ImportCiv3DefensiveBonuses(theBiq, c7Save);
			c7Save.Rules.healRateInFriendlyField = 1;
			c7Save.Rules.healRateInNeutralField = 1;
			c7Save.Rules.healRateInHostileField = 0;
			c7Save.Rules.healRateInCity = 2;
			Dictionary<int, Resource> resourcesByIndex = ImportCiv3Resources(civ3Save.Bic, c7Save);
			SetMapDimensions(civ3Save, c7Save);
			SetWorldWrap(civ3Save, c7Save);

			// Import tiles.  This is similar to, but different from the BIQ version as tile contents may have changed in-game.
			int i = 0;
			foreach (QueryCiv3.Sav.TILE civ3Tile in civ3Save.Tile)
			{
				Civ3ExtraInfo extra = new Civ3ExtraInfo
				{
					BaseTerrainFileID = civ3Tile.TextureFile,
					BaseTerrainImageID = civ3Tile.TextureLocation,
				};
				int x, y;
				(x, y) = GetMapCoordinates(i, civ3Save.Wrld.Width);
				Tile c7Tile = new Tile
				{
					xCoordinate = x,
					yCoordinate = y,
					ExtraInfo = extra,
					baseTerrainTypeKey = c7Save.GameData.terrainTypes[civ3Tile.BaseTerrain].Key,
					baseTerrainType = c7Save.GameData.terrainTypes[civ3Tile.BaseTerrain],
					overlayTerrainTypeKey = c7Save.GameData.terrainTypes[civ3Tile.OverlayTerrain].Key,
					overlayTerrainType = c7Save.GameData.terrainTypes[civ3Tile.OverlayTerrain],
				};
				if (civ3Tile.BonusShield) {
					c7Tile.isBonusShield = true;
				}
				if (civ3Tile.SnowCapped) {
					c7Tile.isSnowCapped = true;
				}
				if (civ3Tile.PineForest) {
					c7Tile.isPineForest = true;
				}
				c7Tile.riverNorth = civ3Tile.RiverNorth;
				c7Tile.riverNortheast = civ3Tile.RiverNortheast;
				c7Tile.riverEast = civ3Tile.RiverEast;
				c7Tile.riverSoutheast = civ3Tile.RiverSoutheast;
				c7Tile.riverSouth = civ3Tile.RiverSouth;
				c7Tile.riverSouthwest = civ3Tile.RiverSouthwest;
				c7Tile.riverWest = civ3Tile.RiverWest;
				c7Tile.riverNorthwest = civ3Tile.RiverNorthwest;
				c7Tile.Resource = resourcesByIndex[civ3Tile.ResourceID];
				c7Tile.ResourceKey = resourcesByIndex[civ3Tile.ResourceID].Key;
				c7Save.GameData.map.tiles.Add(c7Tile);
				i++;
			}
			// This probably doesn't belong here, but not sure where else to put it
			// c7Save.GameData.map.RelativeModPath = civ3Save.MediaBic.Game[0].ScenarioSearchFolders;
			return c7Save;
		}

		/**
		 * defaultBiqPath is used in case some sections (map, rules, player data) are not
		 * present.
		 */
		public static C7SaveFormat ImportBiq(string biqPath, string defaultBiqPath)
		{
			C7SaveFormat c7Save = new C7SaveFormat();

			byte[] biqBytes = Util.ReadFile(biqPath);
			BiqData theBiq = new BiqData(biqBytes);

			ImportUnitPrototypes(theBiq, c7Save);
			ImportCiv3TerrainTypes(theBiq, c7Save);
			ImportCiv3ExperienceLevels(theBiq, c7Save);
			ImportCiv3DefensiveBonuses(theBiq, c7Save);
			c7Save.Rules.healRateInFriendlyField = 1;
			c7Save.Rules.healRateInNeutralField = 1;
			c7Save.Rules.healRateInHostileField = 0;
			c7Save.Rules.healRateInCity = 2;
			Dictionary<int, Resource> resourcesByIndex = ImportCiv3Resources(theBiq, c7Save);
			SetMapDimensions(theBiq, c7Save);
			SetWorldWrap(theBiq, c7Save);

			// Import tiles
			int i = 0;
			foreach (QueryCiv3.Biq.TILE civ3Tile in theBiq.Tile)
			{
				Civ3ExtraInfo extra = new Civ3ExtraInfo
				{
					BaseTerrainFileID = civ3Tile.TextureFile,
					BaseTerrainImageID = civ3Tile.TextureLocation,
				};
				int x, y;
				(x, y) = GetMapCoordinates(i, theBiq.Wmap[0].Width);
				Tile c7Tile = new Tile
				{
					xCoordinate = x,
					yCoordinate = y,
					ExtraInfo = extra,
					baseTerrainTypeKey = c7Save.GameData.terrainTypes[civ3Tile.BaseTerrain].Key,
					baseTerrainType = c7Save.GameData.terrainTypes[civ3Tile.BaseTerrain],
					overlayTerrainTypeKey = c7Save.GameData.terrainTypes[civ3Tile.OverlayTerrain].Key,
					overlayTerrainType = c7Save.GameData.terrainTypes[civ3Tile.OverlayTerrain],
				};
				if (civ3Tile.BonusGrassland) {
					c7Tile.isBonusShield = true;
				}
				if (civ3Tile.SnowCappedMountain) {
					c7Tile.isSnowCapped = true;
				}
				if (civ3Tile.PineForest) {
					c7Tile.isPineForest = true;
				}
				c7Tile.riverNorth = civ3Tile.RiverNorth;
				c7Tile.riverNortheast = civ3Tile.RiverConnectionNortheast;
				c7Tile.riverEast = civ3Tile.RiverEast;
				c7Tile.riverSoutheast = civ3Tile.RiverConnectionSoutheast;
				c7Tile.riverSouth = civ3Tile.RiverSouth;
				c7Tile.riverSouthwest = civ3Tile.RiverConnectionSouthwest;
				c7Tile.riverWest = civ3Tile.RiverWest;
				c7Tile.riverNorthwest = civ3Tile.RiverConnectionNorthwest;
				c7Tile.Resource = resourcesByIndex[civ3Tile.Resource];
				c7Tile.ResourceKey = resourcesByIndex[civ3Tile.Resource].Key;
				c7Save.GameData.map.tiles.Add(c7Tile);
				i++;
			}
			// This probably doesn't belong here, but not sure where else to put it
			// c7Save.GameData.map.RelativeModPath = civ3Save.MediaBic.Game[0].ScenarioSearchFolders;
			return c7Save;
		}

		static (int, int) GetMapCoordinates(int tileIndex, int mapWidth)
		{
			int y = tileIndex / (mapWidth / 2);
			int x = (tileIndex % (mapWidth / 2)) * 2 + (y % 2);
			return (x, y);
		}

		private static Dictionary<int, Resource> ImportCiv3Resources(BiqData biq, C7SaveFormat c7Save)
		{
			int g = 0;
			Dictionary<int, Resource> resourcesByIndex = new Dictionary<int, Resource>(); //will we want to have this for reference later?  Maybe.
			resourcesByIndex[-1] = Resource.NONE;
			foreach (GOOD good in biq.Good) {
				Resource resource = new Resource
				{
					Key = good.Name,
					Index = g,
					Name = good.Name,
					Icon = good.Icon,
					FoodBonus = good.FoodBonus,
					ShieldsBonus = good.ShieldsBonus,
					CommerceBonus = good.CommerceBonus,
					AppearanceRatio = good.AppearanceRatio,
					DisappearanceRatio = good.DisappearanceProbability,
					CivilopediaEntry = good.CivilopediaEntry,
				};
				switch (good.Type) {
					case 0:
						resource.Category = ResourceCategory.BONUS;
						break;
					case 1:
						resource.Category = ResourceCategory.LUXURY;
						break;
					case 2:
						resource.Category = ResourceCategory.STRATEGIC;
						break;
					default:
						Console.WriteLine("WARNING!  Unknown resource category for " + good);
						resource.Category = ResourceCategory.NONE;
						break;
				}
				//TODO: Technologies, once they exist

				c7Save.GameData.Resources.Add(resource);
				resourcesByIndex[g] = resource;
				g++;
			}
			return resourcesByIndex;
		}

		private static void ImportUnitPrototypes(BiqData theBiq, C7SaveFormat c7SaveFormat) {
			//Temporary limiter so you can't build everything out of the gate
			//Once we have technology, we will remove this
			List<string> allowedUnits = new List<string> {"Warrior", "Chariot", "Settler", "Worker", "Catapult", "Galley"};
			foreach (PRTO prto in theBiq.Prto) {
				if (allowedUnits.Contains(prto.Name)) {
					UnitPrototype prototype = new UnitPrototype();
					if (prto.Type == PRTO.TYPE_SEA) {
						prototype.categories.Add("Sea");
					}
					else if (prto.Type == PRTO.TYPE_LAND) {
						prototype.categories.Add("Land");
					}
					else if (prto.Type == PRTO.TYPE_AIR) {
						prototype.categories.Add("Air");
					}
					prototype.name = prto.Name;
					prototype.attack = prto.Attack;
					prototype.defense = prto.Defense;
					prototype.movement = prto.Movement;
					prototype.shieldCost = prto.ShieldCost;
					prototype.populationCost = prto.PopulationCost;
					prototype.bombard = prto.BombardStrength;
					prototype.iconIndex = prto.IconIndex;
					if (prto.BuildCity) {
						prototype.actions.Add("buildCity");
					}
					if (prto.Bombard) {
						prototype.actions.Add("bombard");
					}
					if (prto.SkipTurn) {
						prototype.actions.Add("hold");
					}
					if (prto.Wait) {
						prototype.actions.Add("wait");
					}
					if (prto.Fortify) {
						prototype.actions.Add("fortify");
					}
					if (prto.Disband) {
						prototype.actions.Add("disband");
					}
					if (prto.GoTo) {
						prototype.actions.Add("goTo");
					}
					c7SaveFormat.GameData.unitPrototypes.Add(prototype.name, prototype);
				}
			}
		}

		private static void ImportCiv3TerrainTypes(BiqData theBiq, C7SaveFormat c7Save)
		{
			int civ3Index = 0;
			foreach (TERR terrain in theBiq.Terr) {
				TerrainType c7TerrainType = TerrainType.ImportFromCiv3(civ3Index, terrain);
				c7Save.GameData.terrainTypes.Add(c7TerrainType);
				civ3Index++;
			}
		}

		private static void ImportCiv3ExperienceLevels(BiqData theBiq, C7SaveFormat c7Save)
		{
			if (theBiq.Expr.Length != 4)
				throw new Exception("BIQ data must include four experience levels.");

			Dictionary<string, ExperienceLevel> levelsByKey = new Dictionary<string, ExperienceLevel>();

			foreach (EXPR expr in theBiq.Expr) {
				// Generate a unique key for this level based on its name. If multiple levels have the same name, append apostrophes
				// to the end until the key is unique.
				string key = expr.Name;
				while (levelsByKey.ContainsKey(key))
					key += "'";

				ExperienceLevel level = ExperienceLevel.ImportFromCiv3(key, expr, levelsByKey.Count);
				c7Save.Rules.experienceLevels.Add(level);
				levelsByKey.Add(key, level);

				if (levelsByKey.Count == 2) {
					c7Save.Rules.defaultExperienceLevelKey = key;
					c7Save.Rules.defaultExperienceLevel = level;
				}
			}
		}

		private static void ImportCiv3DefensiveBonuses(BiqData theBiq, C7SaveFormat c7Save)
		{
			c7Save.Rules.fortificationBonus = new StrengthBonus {
				description = "Fortified",
				amount = theBiq.Rule[0].FortificationsDefensiveBonus / 100.0
			};
			c7Save.Rules.riverCrossingBonus = new StrengthBonus {
				description = "Behind river",
				amount = theBiq.Rule[0].RiverDefensiveBonus / 100.0
			};
			c7Save.Rules.cityLevel1DefenseBonus = new StrengthBonus {
				description = "Town",
				amount = theBiq.Rule[0].TownDefenseBonus / 100.0
			};
			c7Save.Rules.cityLevel2DefenseBonus = new StrengthBonus {
				description = "City",
				amount = theBiq.Rule[0].CityDefenseBonus / 100.0
			};
			c7Save.Rules.cityLevel3DefenseBonus = new StrengthBonus {
				description = "Metropolis",
				amount = theBiq.Rule[0].MetropolisDefenseBonus / 100.0
			};
		}

		private static void SetWorldWrap(SavData civ3Save, C7SaveFormat c7Save)
		{
			if (civ3Save != null && civ3Save.Wrld.Height > 0 && civ3Save.Wrld.Width > 0)
			{
				c7Save.GameData.map.wrapHorizontally = civ3Save.Wrld.XWrapping;
				c7Save.GameData.map.wrapVertically = civ3Save.Wrld.YWrapping;
			}
		}

		private static void SetWorldWrap(BiqData biq, C7SaveFormat c7Save)
        {

			if (biq != null && biq.Wmap != null && biq.Wmap.Length > 0)
			{
				c7Save.GameData.map.wrapHorizontally = biq.Wmap[0].XWrapping;
				c7Save.GameData.map.wrapVertically = biq.Wmap[0].YWrapping;
			}
		}

		private static void SetMapDimensions(SavData civ3Save, C7SaveFormat c7Save)
		{
			if (civ3Save != null && civ3Save.Wrld.Height > 0 && civ3Save.Wrld.Width > 0) {
				c7Save.GameData.map.numTilesTall = civ3Save.Wrld.Height;
				c7Save.GameData.map.numTilesWide = civ3Save.Wrld.Width;
			}
		}

		private static void SetMapDimensions(BiqData biq, C7SaveFormat c7Save)
		{
			if (biq != null && biq.Wmap != null && biq.Wmap.Length > 0)
			{
				c7Save.GameData.map.numTilesTall = biq.Wmap[0].Height;
				c7Save.GameData.map.numTilesWide = biq.Wmap[0].Width;
			}
		}
	}
}
