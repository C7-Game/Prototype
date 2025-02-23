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

	public class Civ3ExtraInfo {
		public int BaseTerrainFileID;
		public int BaseTerrainImageID;
	}

	public class ImportCiv3 {
		private SaveGame save;
		private BiqData biq;
		private BiqData defaultBiq;
		private SavData savData;
		private PediaIcons pediaIcons;
		private readonly ID.Factory ids;

		private static ILogger log = Log.ForContext<ImportCiv3>();

		private ImportCiv3() {
			save = new SaveGame();
			ids = new ID.Factory();
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
			ImportBuildings();
			ImportCiv3TerrainTypes();
			ImportCiv3ExperienceLevels();
			ImportCiv3DefensiveBonuses();
			save.HealRates["friendly_field"] = 1;
			save.HealRates["neutral_field"] = 1;
			save.HealRates["hostile_field"] = 0;
			save.HealRates["city"] = 2;
			// save.ScenarioSearchPath = biq?.Game[0].ScenarioSearchFolders;
			ImportBarbarianInfo();
			ImportTechs();
			ImportCitizenTypes();
		}

		public static SaveGame ImportSav(string savePath, string defaultBicPath, Func<string, string> getPediaIconsPath) {
			ImportCiv3 importer = new ImportCiv3();
			return importer.importSav(savePath, defaultBicPath, getPediaIconsPath);
		}

		private SaveGame importSav(string savePath, string defaultBicPath, Func<string, string> getPediaIconsPath) {
			// Get save data reader
			byte[] defaultBicBytes = Util.ReadFile(defaultBicPath);
			savData = new SavData(Util.ReadFile(savePath), defaultBicBytes);
			biq = savData.Bic;
			pediaIcons = new(getPediaIconsPath(biq.Game[0].ScenarioSearchFolders));

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

				(int X, int Y) = GetMapCoordinates(i, savData.Wrld.Width);
				SaveTile tile = new SaveTile{
					id = ids.CreateID("tile"),
					extraInfo = extra,
					X = X,
					Y = Y,
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
				if (civ3Tile.Mine) {
					tile.overlays.Add("mine");
				}
				if (civ3Tile.Irrigation) {
					tile.overlays.Add("irrigation");
				}
				Resource tileResource = resourcesByIndex[civ3Tile.ResourceID];
				if (tileResource != Resource.NONE) {
					tile.resource = tileResource.Key;
				}
				save.Map.tiles.Add(tile);
				for (int playerIndex = 0; playerIndex < save.Players.Count; playerIndex++) {
					if (civ3Tile.ExploredBy[playerIndex]) {
						SavePlayer player = save.Players[playerIndex];
						player.tileKnowledge.Add(new TileLocation(X, Y));
					}
				}
				i++;
			}
			return save;
		}

		/**
		 * defaultBiqPath is used in case some sections (map, rules, player data) are not
		 * present.
		 */
		public static SaveGame ImportBiq(string biqPath, string defaultBiqPath, Func<string, string> getPediaIconsPath) {
			ImportCiv3 importer = new ImportCiv3();
			return importer.importBiq(biqPath, defaultBiqPath, getPediaIconsPath);
		}

		private SaveGame importBiq(string biqPath, string defaultBiqPath, Func<string, string> getPediaIconsPath) {
			biq = BiqData.LoadFile(biqPath);
			defaultBiq = BiqData.LoadFile(defaultBiqPath);
			pediaIcons = new(getPediaIconsPath(biq.Game[0].ScenarioSearchFolders));

			ImportSharedBiqData();
			ImportBicLeaders();
			ImportBicUnits();
			ImportBicCities();

			Dictionary<int, Resource> resourcesByIndex = ImportCiv3Resources();
			SetMapDimensions(biq, save);
			SetWorldWrap(biq, save);

			// Import tiles
			int i = 0;
			foreach (QueryCiv3.Biq.TILE civ3Tile in biq.Tile) {
				(int X, int Y) = GetMapCoordinates(i, biq.Wmap[0].Width);
				Civ3ExtraInfo extra = new Civ3ExtraInfo
				{
					BaseTerrainFileID = civ3Tile.TextureFile,
					BaseTerrainImageID = civ3Tile.TextureLocation,
				};
				SaveTile tile = new SaveTile{
					id = ids.CreateID("tile"),
					extraInfo = extra,
					X = X,
					Y = Y,
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
				if (civ3Tile.Mine) {
					tile.overlays.Add("mine");
				}
				if (civ3Tile.Irrigation) {
					tile.overlays.Add("irrigation");
				}
				Resource tileResource = resourcesByIndex[civ3Tile.Resource];
				if (tileResource != Resource.NONE) {
					tile.resource = tileResource.Key;
				}

				// Some tiles are known ahead of time, like all of europe in age of
				// discovery. Other scenarios set the entire map to be visible.
				//
				// Add those tiles ahead of time.
				if (civ3Tile.FogOfWar != 0 || biq.Game[0].MapVisible == 1) {
					for (int playerIndex = 0; playerIndex < save.Players.Count; playerIndex++) {
						SavePlayer player = save.Players[playerIndex];
						player.tileKnowledge.Add(new TileLocation(X, Y));
					}
				}

				save.Map.tiles.Add(tile);
				i++;
			}

			// The rest of the fog of war is done unit by unit; each unit can see their
			// own tile and the neighbor tiles.
			//
			// This will eventually need to handle hills and other tiles that can see
			// further.
			Dictionary<ID, SavePlayer> playerLookup = new();
			foreach (SavePlayer player in save.Players) {
				playerLookup[player.id] = player;
			}

			foreach (SaveUnit unit in save.Units) {
				SavePlayer player = playerLookup[unit.owner];
				player.tileKnowledge.Add(unit.currentLocation);
				foreach (TileDirection direction in Enum.GetValues(typeof(TileDirection))) {
					player.tileKnowledge.Add(Tile.NeighborCoordinate(unit.currentLocation, direction));
				}
			}
			foreach (SaveCity city in save.Cities) {
				SavePlayer player = playerLookup[city.owner];
				player.tileKnowledge.Add(city.location);
				foreach (TileDirection direction in Enum.GetValues(typeof(TileDirection))) {
					player.tileKnowledge.Add(Tile.NeighborCoordinate(city.location, direction));
				}
			}

			return save;
		}

		static (int, int) GetMapCoordinates(int tileIndex, int mapWidth) {
			int Y = tileIndex / (mapWidth / 2);
			int X = tileIndex % (mapWidth / 2) * 2 + (Y % 2);
			return (X, Y);
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
					noun = race.Noun,
					leader = race.LeaderName,
					leaderGender = race.LeaderGender == 0 ? Gender.Male : Gender.Female,
					colorIndex = race.DefaultColor,
				};
				foreach (RACE_City city in theBiq.RaceCityName[i]) {
					civ.cityNames.Add(city.Name);
				}
				save.Civilizations.Add(civ);
				i++;
			}
		}

		private void ImportBicLeaders() {
			BiqData theBiq = biq.Race is null ? defaultBiq : biq;

			// Make a player for each civ. The barbarians are always civ 0.
			for (int i = 0; i < save.Civilizations.Count; ++i) {
				save.Players.Add(MakeSavePlayerFromCiv(save.Civilizations[i],
									   /*isBarbarian=*/i == 0,
									   /*isHuman=*/false,
									   /*cityNameIndex=*/0,
									   /*era=*/""));
			}

			// Now fill in the rest of the data using the leader struct.
			bool foundHuman = false;
			int leadIndex = 0;
			foreach (LEAD lead in theBiq.Lead) {
				SavePlayer player = save.Players[lead.Civ];

				// Put the player in the correct starting era.
				player.eraCivilopediaName = theBiq.Eras[lead.InitialEra].CivilopediaEntry;

				// Give the correct amount of starting gold.
				player.gold = lead.StartCash;

				// The game starts out with 50% on the science slider and 0% on
				// the luxury slider.
				player.scienceRate = 5;
				player.taxRate = 5;
				player.luxuryRate = 0;

				// Add the starting techs for scenarios.
				if (theBiq.LeadTech != null) {
					for (int j = 0; j < theBiq.LeadTech[leadIndex].Length; ++j) {
						player.knownTechs.Add(save.Techs[theBiq.LeadTech[leadIndex][j]].id);
					}
				}

				// Mark the first human playable civ as the human player.
				if (lead.HumanPlayer == 1 && !foundHuman) {
					player.human = true;
					foundHuman = true;
				}

				++leadIndex;
			}
		}

		private void ImportSavLeaders() {
			BiqData theBiq = biq.Eras is null ? defaultBiq : biq;
			int i = 0;
			foreach (QueryCiv3.Sav.LEAD leader in savData.Lead) {
				if (leader.RaceID == -1) {
					continue; // can probably break here
				}
				Civilization civ = save.Civilizations[leader.RaceID];
				SavePlayer player = MakeSavePlayerFromCiv(civ,
										  /*isBarbarian=*/i == 0,
										  /*isHuman=*/i == 1,
										  /*cityNameIndex=*/leader.FoundedCities,
										  /*era=*/theBiq.Eras[leader.Era].CivilopediaEntry);

				// Record what the player is currently researching.
				if (leader.Researching > -1) {
					player.currentlyResearchedTech = save.Techs[leader.Researching].id;
				}

				// Record any techs that this player knows.
				for (int k = 0; k < savData.KnownTechFlags.Length; ++k) {
					if (savData.KnownTechFlags[k][i]) {
						player.knownTechs.Add(save.Techs[k].id);
					}
				}

				player.gold = leader.Gold;
				player.beakers = leader.Beakers;
				player.turnsResearched = leader.TurnsResearched;
				player.scienceRate = leader.ScienceRate;
				player.luxuryRate = leader.LuxuryRate;
				player.taxRate = leader.TaxRate;

				save.Players.Add(player);
				i++;
			}
		}

		private SavePlayer MakeSavePlayerFromCiv(Civilization civ, bool isBarbarian, bool isHuman, int cityNameIndex, string era) {
			return new SavePlayer {
				id = ids.CreateID("player"),
				colorIndex = civ.colorIndex,
				barbarian = isBarbarian,
				human = isHuman,
				civilization = civ.name,
				hasPlayedCurrentTurn = false, // TODO: find how this information is stored in a .sav
				cityNameIndex = cityNameIndex,
				eraCivilopediaName = era,
			};
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

		private void ImportBicUnits() {
			// TODO: This doesn't account for default starting units.
			BiqData theBiq = biq.Unit is null ? defaultBiq : biq;

			foreach (UNIT unit in theBiq.Unit) {
				if (unit.Owner >= save.Players.Count) {
					continue;
				}

				// The owner index is into the list of civs, and we have a 1:1
				// mapping of players and civs.
				SavePlayer player = save.Players[unit.Owner];

				PRTO prototype = theBiq.Prto[unit.UnitType];
				ExperienceLevel experience = save.ExperienceLevels[unit.ExperienceLevel];
				SaveUnit saveUnit = new SaveUnit{
					id = ids.CreateID(prototype.Name),
					owner = player.id,
					prototype = prototype.Name,
					currentLocation = new TileLocation(unit.X, unit.Y),
					previousLocation = new TileLocation(unit.X, unit.Y),
					experience = experience.key,
					hitPointsRemaining = experience.baseHitPoints, // TODO: include bonus hitpoints from unit type
					movePointsRemaining = (float)prototype.Movement,
				};
				save.Units.Add(saveUnit);
			}
		}

		private void ImportSavCities() {
			for (int i = 0; i < savData.City.Length; ++i) {
				QueryCiv3.Sav.CITY city = savData.City[i];
				SavePlayer owner = save.Players[city.Owner];

				var (producible, producibleType) = CityToProducible(city);

				SaveCity saveCity = new SaveCity{
					id = ids.CreateID("city"),
					owner = owner.id,
					location = new TileLocation(city.X, city.Y),
					capital = i == savData.Lead[city.Owner].CapitalCity,
					producible = producible,
					producibleType = producibleType,
					name = city.Name,
					size = city.Popd.CitizenCount,
					shieldsStored = city.ShieldsCollected,
					foodStored = city.TotalFood,
					foodNeededToGrow = 20, // HACK: don't know where to find this
				};

				List<int> culturePerLeader = city.GetCulturePerLeader();
				for (int j = 0; j < 32; ++j) {
					if (culturePerLeader[j] > 0 || j == city.Owner) {
						saveCity.perPlayerCulture.Add(save.Players[j].id.ToString(), culturePerLeader[j]);
					}
				}

				foreach (QueryCiv3.Sav.CTZN ctzn in savData.CityCtzn[i]) {
					if (ctzn.Type == 4) {  // Specialist
						SaveCityResident scr = new();
						scr.city = saveCity.id;
						scr.nationality = save.Civilizations[ctzn.Nationality].name;
						scr.citizenType = save.CitizenTypes.Find(x => x.SpecialistIndex == ctzn.SpecialistType).Id;
						saveCity.residents.Add(scr);
					} else if (ctzn.TileWorked == 0) {
						// TODO: handle resistors
					} else {
						SaveCityResident scr = new();
						scr.city = saveCity.id;
						scr.tileWorked = GetTileFromSpiral(saveCity.location, ctzn.TileWorked);
						scr.nationality = save.Civilizations[ctzn.Nationality].name;
						scr.citizenType = save.CitizenTypes.Find(x => x.IsDefaultCitizen).Id;
						saveCity.residents.Add(scr);
					}
				}
				save.Cities.Add(saveCity);
			}
		}

		private (string, ProducibleType) CityToProducible(QueryCiv3.Sav.CITY city) {
			PRTO[] unitPrototypes = biq.Prto ?? defaultBiq.Prto;

			return city.ConstructingType switch {
				0 => ("Worker", ProducibleType.UNIT), // TODO: Wealth production is not implemented yet
				1 => (save.Buildings[city.Constructing].name, ProducibleType.BUILDING),
				2 => (unitPrototypes[city.Constructing].Name, ProducibleType.UNIT),
				_ => throw new NotImplementedException()
			};
		}

		private void ImportBicCities() {
			BiqData theBiq = biq.City is null ? defaultBiq : biq;

			foreach (CITY city in theBiq.City) {
				// The owner index is into the list of civs, and we have a 1:1
				// mapping of players and civs.
				SavePlayer player = save.Players[city.Owner];

				SaveCity saveCity = new SaveCity{
					id = ids.CreateID("city"),
					owner = player.id,
					location = new TileLocation(city.X, city.Y),
					capital = city.HasPalace != 0,
					// TODO: try and get this from the unit prototype
					producible = "Worker",
					producibleType = ProducibleType.UNIT,
					name = city.Name,
					size = city.Size,
					shieldsStored = 0,
					foodStored = 0,
					foodNeededToGrow = 20, // HACK: don't know where to find this
				};
				saveCity.perPlayerCulture.Add(player.id.ToString(), city.Culture);

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
				prototype.artName = pediaIcons.GetUnitArtName(prto.CivilopediaEntry);
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
				if (prto.BuildMine) {
					prototype.actions.Add(C7Action.UnitBuildMine);
				}
				if (prto.Irrigate) {
					prototype.actions.Add(C7Action.UnitIrrigate);
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

		private void ImportBuildings() {
			BLDG[] Bldg = biq.Bldg ?? defaultBiq.Bldg;

			foreach (BLDG bldg in Bldg) {
				Building building = new() {
					name=bldg.Name,
					shieldCost=bldg.Cost * 10, // In Civ3 files, building costs are stored at 1/10th of their actual value
					populationCost=0, // In Civ3, a building cannot have a population cost
				};
				save.Buildings.Add(building);
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

		private void ImportTechs() {
			BiqData theBiq = biq.Tech is null ? defaultBiq : biq;

			// Pass one: create the techs without prereqs.
			for (int i = 0; i < theBiq.Tech.Length; ++i) {
				TECH t = theBiq.Tech[i];

				SaveTech st = new() {
					id = ids.CreateID("tech"),
					Name = t.Name,
					CivilopediaEntry = t.CivilopediaEntry,
					Cost = t.Cost,
					EraCivilopediaName = t.Era == -1 ? "Hidden" : theBiq.Eras[t.Era].CivilopediaEntry,
					SmallIconPath = t.Era == -1 ? "" : pediaIcons.GetTechIconPath(t.CivilopediaEntry),
					X = t.X,
					Y = t.Y
				};
				save.Techs.Add(st);
			}

			// Pass two: set up the prereqs now that we can index into the list
			// of techs.
			for (int i = 0; i < theBiq.Tech.Length; ++i) {
				TECH t = theBiq.Tech[i];
				SaveTech st = save.Techs[i];

				if (t.Prerequisite1 > -1) {
					st.Prerequisites.Add(save.Techs[t.Prerequisite1].id);
				}
				if (t.Prerequisite2 > -1) {
					st.Prerequisites.Add(save.Techs[t.Prerequisite2].id);
				}
				if (t.Prerequisite3 > -1) {
					st.Prerequisites.Add(save.Techs[t.Prerequisite3].id);
				}
				if (t.Prerequisite4 > -1) {
					st.Prerequisites.Add(save.Techs[t.Prerequisite3].id);
				}
			}

			// Now that we have ids for all the techs, distribute the free techs
			for (int i = 0; i < save.Civilizations.Count; ++i) {
				Civilization sc = save.Civilizations[i];
				RACE race = theBiq.Race[i];

				if (race.FreeTech1 > -1) {
					sc.startingTechs.Add(save.Techs[race.FreeTech1].id);
				}
				if (race.FreeTech2 > -1) {
					sc.startingTechs.Add(save.Techs[race.FreeTech2].id);
				}
				if (race.FreeTech3 > -1) {
					sc.startingTechs.Add(save.Techs[race.FreeTech3].id);
				}
				if (race.FreeTech4 > -1) {
					sc.startingTechs.Add(save.Techs[race.FreeTech4].id);
				}

				// Remove any invalid starting techs. Some scenarios like
				// Fall of Rome give starting techs without giving all of the
				// prereqs, so they should be ignored.
				sc.startingTechs.RemoveWhere(t => {
					SaveTech st = save.Techs.Find(x => x.id == t);
					foreach (ID prereqId in st.Prerequisites) {
						if (!sc.startingTechs.Contains(prereqId)) {
							return true;
						}
					}
					return false;
				});
			}
		}

		private void ImportCitizenTypes() {
			BiqData theBiq = biq.Ctzn is null ? defaultBiq : biq;

			for (int i = 0; i < theBiq.Ctzn.Length; ++i) {
				CTZN c = theBiq.Ctzn[i];

				CitizenType ct = new() {
					Id = ids.CreateID("CitizenType"),
					IsDefaultCitizen = c.DefaultCitizen == 1,
					SingularName = c.SingularName,
					CivilopediaEntry = c.CivilopediaEntry,
					PluralName = c.PluralName,
					Luxuries = c.Luxuries,
					Research = c.Research,
					Taxes = c.Taxes,
					Corruption = c.Corruption,
					Construction = c.Construction
				};
				if (!ct.IsDefaultCitizen) {
					ct.SpecialistIndex = i;
				}
				if (c.Prerequisite > -1) {
					ct.PrerequisiteTech = save.Techs[c.Prerequisite].id;
				}

				save.CitizenTypes.Add(ct);
			}
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

		// The position of citizens is encoded in a single byte as positions in
		// a spiral around the city, starting with the NE tile and going
		// clockwise. After the inner spiral, it continues with the top of the
		// NE outer spiral. Here's a crude ASCII diagram of this.
		//
		//                      <   8  >
		//                  <   7  ><   1  >
		//              <   6  >< City ><   2  >
		//                  <   5  ><   3  >
		//                      <   4  >
		//
		//                  <  20  ><  9  >
		//              <  19  >        <  10  >
		//          <  18  >                <  11 >
		//                      < City >
		//          <  17  >                <  12  >
		//              <  16  >        <  13  >
		//                  <  15  ><  14  >
		private static TileLocation GetTileFromSpiral(TileLocation start, int spiral) {
			return spiral switch {
				// Inner circle.
				1 => Tile.NeighborCoordinate(start, TileDirection.NORTHEAST),
				2 => Tile.NeighborCoordinate(start, TileDirection.EAST),
				3 => Tile.NeighborCoordinate(start, TileDirection.SOUTHEAST),
				4 => Tile.NeighborCoordinate(start, TileDirection.SOUTH),
				5 => Tile.NeighborCoordinate(start, TileDirection.SOUTHWEST),
				6 => Tile.NeighborCoordinate(start, TileDirection.WEST),
				7 => Tile.NeighborCoordinate(start, TileDirection.NORTHWEST),
				8 => Tile.NeighborCoordinate(start, TileDirection.NORTH),

				// Outer circle, to the NE
				9 => Tile.NeighborCoordinate(Tile.NeighborCoordinate(start, TileDirection.NORTHEAST), TileDirection.NORTH),
				10 => Tile.NeighborCoordinate(Tile.NeighborCoordinate(start, TileDirection.NORTHEAST), TileDirection.NORTHEAST),
				11 => Tile.NeighborCoordinate(Tile.NeighborCoordinate(start, TileDirection.NORTHEAST), TileDirection.EAST),

				// Outer circle, to the SE
				12 => Tile.NeighborCoordinate(Tile.NeighborCoordinate(start, TileDirection.SOUTHEAST), TileDirection.EAST),
				13 => Tile.NeighborCoordinate(Tile.NeighborCoordinate(start, TileDirection.SOUTHEAST), TileDirection.SOUTHEAST),
				14 => Tile.NeighborCoordinate(Tile.NeighborCoordinate(start, TileDirection.SOUTHEAST), TileDirection.SOUTH),

				// Outer circle, to the SW
				15 => Tile.NeighborCoordinate(Tile.NeighborCoordinate(start, TileDirection.SOUTHWEST), TileDirection.SOUTH),
				16 => Tile.NeighborCoordinate(Tile.NeighborCoordinate(start, TileDirection.SOUTHWEST), TileDirection.SOUTHWEST),
				17 => Tile.NeighborCoordinate(Tile.NeighborCoordinate(start, TileDirection.SOUTHWEST), TileDirection.WEST),

				// Outer circle, to the NW
				18 => Tile.NeighborCoordinate(Tile.NeighborCoordinate(start, TileDirection.NORTHWEST), TileDirection.WEST),
				19 => Tile.NeighborCoordinate(Tile.NeighborCoordinate(start, TileDirection.NORTHWEST), TileDirection.NORTHWEST),
				20 => Tile.NeighborCoordinate(Tile.NeighborCoordinate(start, TileDirection.NORTHWEST), TileDirection.NORTH),

				_ => throw new ArgumentOutOfRangeException("Invalid spiral value" + spiral),
			};

		}
	}
}
