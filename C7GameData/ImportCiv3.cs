using System;
using System.Collections.Generic;
using System.Linq;
using Serilog;
using QueryCiv3;
using QueryCiv3.Biq;
using C7GameData.Save;

/*
  This will read a Civ3 sav into C7 native format for immediate use or saving to native JSON save
*/

namespace C7GameData {

	public class Civ3ExtraInfo
	{
		public int BaseTerrainFileID;
		public int BaseTerrainImageID;
	}

	public class ImportCiv3 {
		private SaveGame save;
		private BiqData biq;
		private BiqData defaultBiq;
		private SavData savData;
		private readonly IDFactory ids;

		private static ILogger log = Log.ForContext<ImportCiv3>();

		private ImportCiv3() {
			save = new SaveGame();
			ids = new IDFactory();
		}

		/// <summary>
		/// Items loaded from the BIQ and used the same way in both the SAV and BIQ should generally go here.
		/// This excludes items that can change mid-game, such as tiles (which may be chopped, roaded, etc.).
		/// </summary>
		/// <param name="theBiq">Source BIQ</param>
		/// <param name="c7Save">Destination C7 in-memory structure</param>
		private void ImportSharedBiqData() {
			ImportRaces();
			ImportUnitPrototypes();
			ImportCiv3TerrainTypes();
			ImportCiv3ExperienceLevels();
			ImportCiv3DefensiveBonuses();
			save.HealRates["friendly_field"] = 1;
			save.HealRates["neutral_field"] = 1;
			save.HealRates["hostile_field"] = 0;
			save.HealRates["city"] = 2;
			// save.ScenarioSearchPath = biq?.Game[0].ScenarioSearchFolders;
			ImportBarbarianInfo();
		}

		public static SaveGame ImportSav(string savePath, string defaultBicPath) {
			ImportCiv3 importer = new ImportCiv3();
			return importer.importSav(savePath, defaultBicPath);
		}

		private SaveGame importSav(string savePath, string defaultBicPath) {
			// Get save data reader
			byte[] defaultBicBytes = Util.ReadFile(defaultBicPath);
			savData = new SavData(Util.ReadFile(savePath), defaultBicBytes);
			biq = savData.Bic;

			ImportSharedBiqData();
			ImportSavLeaders();
			ImportSavUnits();
			ImportSavCities();

			Dictionary<int, Resource> resourcesByIndex = ImportCiv3Resources();
			SetMapDimensions(savData, save);
			SetWorldWrap(savData, save);

			// Import tiles.  This is similar to, but different from the BIQ version as tile contents may have changed in-game.
			int i = 0;
			foreach (QueryCiv3.Sav.TILE civ3Tile in savData.Tile) {
				// TODO: Civ3ExtraInfo can be removed when migrating to rendering via tilemap
				Civ3ExtraInfo extra = new Civ3ExtraInfo
				{
					BaseTerrainFileID = civ3Tile.TextureFile,
					BaseTerrainImageID = civ3Tile.TextureLocation,
				};

				(int x, int y) = GetMapCoordinates(i, savData.Wrld.Width);
				SaveTile tile = new SaveTile{
					id = ids.CreateID("tile"),
					extraInfo = extra,
					x = x,
					y = y,
					baseTerrain = save.TerrainTypes[civ3Tile.BaseTerrain].Key,
					overlayTerrain = save.TerrainTypes[civ3Tile.OverlayTerrain].Key,
				};
				if (civ3Tile.BonusShield) {
					tile.features.Add("bonusShield");
				}
				if (civ3Tile.SnowCapped) {
					tile.features.Add("snowCapped");
				}
				if (civ3Tile.PineForest) {
					tile.features.Add("pineForest");
				}
				if (civ3Tile.RiverNorth) {
					tile.features.Add("riverNorth");
				}
				if (civ3Tile.RiverNortheast) {
					tile.features.Add("riverNortheast");
				}
				if (civ3Tile.RiverEast) {
					tile.features.Add("riverEast");
				}
				if (civ3Tile.RiverSoutheast) {
					tile.features.Add("riverSoutheast");
				}
				if (civ3Tile.RiverSouth) {
					tile.features.Add("riverSouth");
				}
				if (civ3Tile.RiverSouthwest) {
					tile.features.Add("riverSouthwest");
				}
				if (civ3Tile.RiverWest) {
					tile.features.Add("riverWest");
				}
				if (civ3Tile.RiverNorthwest) {
					tile.features.Add("riverNorthwest");
				}
				if (civ3Tile.Road) {
					tile.overlays.Add("road");
				}
				if (civ3Tile.Railroad) {
					tile.overlays.Add("railroad");
				}
				Resource tileResource = resourcesByIndex[civ3Tile.ResourceID];
				if (tileResource != Resource.NONE) {
					tile.resource = tileResource.Key;
				}
				save.Map.tiles.Add(tile);
				for (int playerIndex = 0; playerIndex < save.Players.Count; playerIndex++) {
					if (civ3Tile.ExploredBy[playerIndex]) {
						SavePlayer player = save.Players[playerIndex];
						player.tileKnowledge.Add(new TileLocation(x, y));
					}
				}
				i++;
			}
			// This probably doesn't belong here, but not sure where else to put it
			// c7Save.GameData.map.RelativeModPath = civ3Save.MediaBic.Game[0].ScenarioSearchFolders;
			return save;
		}

		/**
		 * defaultBiqPath is used in case some sections (map, rules, player data) are not
		 * present.
		 */
		public static SaveGame ImportBiq(string biqPath, string defaultBiqPath) {
			ImportCiv3 importer = new ImportCiv3();
			return importer.importBiq(biqPath, defaultBiqPath);
		}

		private SaveGame importBiq(string biqPath, string defaultBiqPath) {
			biq = BiqData.LoadFile(biqPath);
			defaultBiq = BiqData.LoadFile(defaultBiqPath);

			ImportSharedBiqData();
			Dictionary<int, Resource> resourcesByIndex = ImportCiv3Resources();
			SetMapDimensions(biq, save);
			SetWorldWrap(biq, save);

			// Import tiles
			int i = 0;
			foreach (TILE civ3Tile in biq.Tile) {
				(int x, int y) = GetMapCoordinates(i, biq.Wmap[0].Width);
				SaveTile tile = new SaveTile{
					x = x,
					y = y,
					baseTerrain = save.TerrainTypes[civ3Tile.BaseTerrain].Key,
					overlayTerrain = save.TerrainTypes[civ3Tile.OverlayTerrain].Key,
				};
				if (civ3Tile.BonusGrassland) {
					tile.features.Add("bonusShield");
				}
				if (civ3Tile.SnowCappedMountain) {
					tile.features.Add("snowCapped");
				}
				if (civ3Tile.PineForest) {
					tile.features.Add("pineForest");
				}
				if (civ3Tile.RiverNorth) {
					tile.features.Add("riverNorth");
				}
				if (civ3Tile.RiverConnectionNortheast) {
					tile.features.Add("riverNortheast");
				}
				if (civ3Tile.RiverEast) {
					tile.features.Add("riverEast");
				}
				if (civ3Tile.RiverConnectionSoutheast) {
					tile.features.Add("riverSoutheast");
				}
				if (civ3Tile.RiverSouth) {
					tile.features.Add("riverSouth");
				}
				if (civ3Tile.RiverConnectionSouthwest) {
					tile.features.Add("riverSouthwest");
				}
				if (civ3Tile.RiverWest) {
					tile.features.Add("riverWest");
				}
				if (civ3Tile.RiverConnectionNorthwest) {
					tile.features.Add("riverNorthwest");
				}
				if (civ3Tile.Road) {
					tile.overlays.Add("road");
				}
				if (civ3Tile.Railroad) {
					tile.overlays.Add("railroad");
				}
				Resource tileResource = resourcesByIndex[civ3Tile.Resource];
				if (tileResource != Resource.NONE) {
					tile.resource = tileResource.Key;
				}
				save.Map.tiles.Add(tile);
				i++;
			}
			// This probably doesn't belong here, but not sure where else to put it
			// c7Save.GameData.map.RelativeModPath = civ3Save.MediaBic.Game[0].ScenarioSearchFolders;
			return save;
		}

		static (int, int) GetMapCoordinates(int tileIndex, int mapWidth) {
			int y = tileIndex / (mapWidth / 2);
			int x = tileIndex % (mapWidth / 2) * 2 + (y % 2);
			return (x, y);
		}

		private Dictionary<int, Resource> ImportCiv3Resources() {
			GOOD[] Good = biq?.Good ?? defaultBiq.Good;
			int g = 0;
			Dictionary<int, Resource> resourcesByIndex = new Dictionary<int, Resource>(); //will we want to have this for reference later?  Maybe.
			resourcesByIndex[-1] = Resource.NONE;
			foreach (GOOD good in Good) {
				Resource resource = new Resource {
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
					Category = good.Type switch {
						0 => ResourceCategory.BONUS,
						1 => ResourceCategory.LUXURY,
						2 => ResourceCategory.STRATEGIC,
						_ => ResourceCategory.NONE,
					}
				};
				if (resource.Category == ResourceCategory.NONE) {
					log.Warning("WARNING!  Unknown resource category for " + good);
				}
				//TODO: Technologies, once they exist

				save.Resources.Add(resource);
				resourcesByIndex[g] = resource;
				g++;
			}
			return resourcesByIndex;
		}

		private void ImportRaces() {
			BiqData theBiq = biq.Race is null ? defaultBiq : biq;
			int i = 0;
			foreach (RACE race in theBiq.Race) {
				Civilization civ = new Civilization{
					name = race.Name,
					leader = race.LeaderName,
					leaderGender = race.LeaderGender == 0 ? Gender.Male : Gender.Female,
					color = race.DefaultColor,
				};
				foreach (RACE_City city in theBiq.RaceCityName[i]) {
					civ.cityNames.Add(city.Name);
				}
				save.Civilizations.Add(civ);
				i++;
			}
		}

		private void ImportSavLeaders() {
			int i = 0;
			foreach (QueryCiv3.Sav.LEAD leader in savData.Lead) {
				if (leader.RaceID == -1) {
					continue; // can probably break here
				}
				Civilization civ = save.Civilizations[leader.RaceID];
				save.Players.Add(new SavePlayer{
					id = ids.CreateID("player"),
					color = (uint)civ.color,
					barbarian = i == 0,
					human = i == 1,
					civilization = civ.name,
					hasPlayedCurrentTurn = false, // TODO: find how this information is stored in a .sav
					cityNameIndex = leader.FoundedCities,
				});
				i++;
			}
		}

		private void ImportSavUnits() {
			foreach (QueryCiv3.Sav.UNIT unit in savData.Unit) {
				if (unit.OwnerID < 0 || unit.OwnerID >= save.Players.Count) {
					continue;
				}
				SavePlayer player = save.Players[unit.OwnerID];
				PRTO prototype = savData.Bic.Prto[unit.UnitType];
				ExperienceLevel experience = save.ExperienceLevels[unit.ExperienceLevel];
				SaveUnit saveUnit = new SaveUnit{
					id = ids.CreateID(prototype.Name),
					owner = player.id,
					prototype = prototype.Name,
					currentLocation = new TileLocation(unit.X, unit.Y),
					previousLocation = new TileLocation(unit.PreviousX, unit.PreviousY),
					experience = experience.key,
					hitPointsRemaining = experience.baseHitPoints - unit.Damage, // TODO: include bonus hitpoints from unit type
					movePointsRemaining = (float)prototype.Movement - (unit.MovementUsed / 3f),
				};
				if (unit.Fortified) {
					saveUnit.action = "fortified";
				}
				save.Units.Add(saveUnit);
			}
		}

		private void ImportSavCities() {
			foreach (QueryCiv3.Sav.CITY city in savData.City) {
				SavePlayer owner = save.Players[city.Owner];
				SaveCity saveCity = new SaveCity{
					id = ids.CreateID("city"),
					owner = owner.id,
					location = new TileLocation(city.X, city.Y),
					// producible = city.Constructing // TODO: lookup building or unit prototype
					producible = "Warrior",
					name = city.Name,
					size = city.Popd.CitizenCount,
					shieldsStored = city.ShieldsCollected,
					foodStored = city.TotalFood,
					foodNeededToGrow = 20, // HACK: don't know where to find this
					// residents = city.Ppod // TODO: load tiles worked from PPOD
				};
				save.Cities.Add(saveCity);
			}
		}

		private void ImportUnitPrototypes() {
			PRTO[] Prto = biq.Prto ?? defaultBiq.Prto;
			foreach (PRTO prto in Prto) {
				UnitPrototype prototype = new UnitPrototype();
				if (prto.Type == PRTO.TYPE_SEA) {
					prototype.categories.Add("Sea");
				} else if (prto.Type == PRTO.TYPE_LAND) {
					prototype.categories.Add("Land");
				} else if (prto.Type == PRTO.TYPE_AIR) {
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
					prototype.actions.Add(C7Action.UnitBuildCity);
				}
				if (prto.BuildRoad) {
					prototype.actions.Add(C7Action.UnitBuildRoad);
				}
				if (prto.Bombard) {
					prototype.actions.Add(C7Action.UnitBombard);
				}
				if (prto.SkipTurn) {
					prototype.actions.Add(C7Action.UnitHold);
				}
				if (prto.Wait) {
					prototype.actions.Add(C7Action.UnitWait);
				}
				if (prto.Fortify) {
					prototype.actions.Add(C7Action.UnitFortify);
				}
				if (prto.Disband) {
					prototype.actions.Add(C7Action.UnitDisband);
				}
				if (prto.GoTo) {
					prototype.actions.Add(C7Action.UnitGoto);
				}
				//Temporary check until #329/#330 are finished
				if (!save.UnitPrototypes.Where(p => p.name == prototype.name).Any()) {
					save.UnitPrototypes.Add(prototype);
				}
			}
		}

		private void ImportCiv3TerrainTypes() {
			TERR[] Terr = biq.Terr ?? defaultBiq.Terr;
			int civ3Index = 0;
			foreach (TERR terrain in Terr) {
				TerrainType c7TerrainType = TerrainType.ImportFromCiv3(civ3Index, terrain);
				save.TerrainTypes.Add(c7TerrainType);
				civ3Index++;
			}
		}

		private void ImportCiv3ExperienceLevels() {
			EXPR[] Expr = biq.Expr ?? defaultBiq.Expr;
			if (Expr.Length != 4) {
				throw new Exception("BIQ data must include four experience levels.");
			}

			Dictionary<string, ExperienceLevel> levelsByKey = new Dictionary<string, ExperienceLevel>();

			foreach (EXPR expr in Expr) {
				// Generate a unique key for this level based on its name. If multiple levels have the same name, append apostrophes
				// to the end until the key is unique.
				string key = expr.Name;
				while (levelsByKey.ContainsKey(key)) {
					key += "'";
				}

				ExperienceLevel level = ExperienceLevel.ImportFromCiv3(key, expr, levelsByKey.Count);
				save.ExperienceLevels.Add(level);
				levelsByKey.Add(key, level);

				if (levelsByKey.Count == 2) {
					save.DefaultExperienceLevel = key;
				}
			}
		}

		private void ImportCiv3DefensiveBonuses() {
			RULE Rule = biq?.Rule?[0] ?? defaultBiq.Rule[0];
			save.StrengthBonuses.Add(new StrengthBonus {
				description = "Fortified",
				amount = Rule.FortificationsDefensiveBonus / 100.0
			});

			save.StrengthBonuses.Add(new StrengthBonus {
				description = "Behind river",
				amount = Rule.RiverDefensiveBonus / 100.0
			});

			save.StrengthBonuses.Add(new StrengthBonus {
				description = "Town",
				amount = Rule.TownDefenseBonus / 100.0
			});

			save.StrengthBonuses.Add(new StrengthBonus {
				description = "City",
				amount = Rule.CityDefenseBonus / 100.0
			});

			save.StrengthBonuses.Add(new StrengthBonus {
				description = "Metropolis",
				amount = Rule.MetropolisDefenseBonus / 100.0
			});
		}

		private void ImportBarbarianInfo() {
			RULE Rule = biq?.Rule?[0] ?? defaultBiq.Rule[0];
			PRTO[] Prto = biq?.Prto ?? defaultBiq.Prto;
			BarbarianInfo barbInfo = save.BarbarianInfo;
			// TODO: this relies on the unit prototypes in SaveGame being
			// at the same indices as in PRTO...
			barbInfo.basicBarbarianIndex = Rule.BasicBarbarianUnitType;
			barbInfo.advancedBarbarianIndex = Rule.AdvancedBarbarianUnitType;
			barbInfo.barbarianSeaUnitIndex = Rule.BarbarianSeaUnitType;
		}

		private static void SetWorldWrap(SavData civ3Save, SaveGame save) {
			if (civ3Save is not null && civ3Save.Wrld.Height > 0 && civ3Save.Wrld.Width > 0) {
				save.Map.wrapHorizontally = civ3Save.Wrld.XWrapping;
				save.Map.wrapVertically = civ3Save.Wrld.YWrapping;
			}
		}

		private static void SetWorldWrap(BiqData biq, SaveGame save) {
			if (biq is not null && biq.Wmap is not null && biq.Wmap.Length > 0) {
				save.Map.wrapHorizontally = biq.Wmap[0].XWrapping;
				save.Map.wrapVertically = biq.Wmap[0].YWrapping;
			}
		}

		private static void SetMapDimensions(SavData civ3Save, SaveGame save) {
			if (civ3Save is not null && civ3Save.Wrld.Height > 0 && civ3Save.Wrld.Width > 0) {
				save.Map.tilesTall = civ3Save.Wrld.Height;
				save.Map.tilesWide = civ3Save.Wrld.Width;
			}
		}

		private static void SetMapDimensions(BiqData biq, SaveGame save) {
			if (biq is not null && biq.Wmap is not null && biq.Wmap.Length > 0) {
				save.Map.tilesTall = biq.Wmap[0].Height;
				save.Map.tilesWide = biq.Wmap[0].Width;
			}
		}
	}
}
