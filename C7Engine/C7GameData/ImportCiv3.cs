using System;
using System.Collections.Generic;
using System.Linq;
using Serilog;
using QueryCiv3;
using QueryCiv3.Biq;
using C7GameData.Save;
using System.Reflection;

/*
  This will read a Civ3 sav into C7 native format for immediate use or saving to native JSON save
*/

namespace C7GameData {

	public class Civ3ExtraInfo {
		public int BaseTerrainFileID;
		public int BaseTerrainImageID;
	}

	public enum TerraformKey {
		BuildRoad,
		BuildRailroad,
		BuildMine,
		BuildFortress,
		ClearDamage,
		BuildAirfield,
		BuildRadarTower,
		BuildOutpost,
		BuildBarricade,
		Irrigate,
		ClearWetlands,
		ClearForest,
		PlantForest
	}

	public class ImportCiv3 {
		private SaveGame save;
		private BiqData biq;
		private BiqData defaultBiq;
		private SavData savData;
		private PediaIcons pediaIcons;
		private readonly ID.Factory ids;
		private Dictionary<TerraformKey, ID> terraformIdByCiv3Key = [];

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
			save.TerrainImprovements = SaveTerrainImprovement.Civ3Improvements().ToList();

			ImportRaces();
			ImportTechs();
			ImportCiv3Resources();
			ImportTerraforms();
			ImportUnitPrototypes();
			ImportUniqueUnitReplacements();
			ImportUnitUpgrades();
			ImportBuildings();
			ImportCiv3TerrainTypes();
			ImportCiv3ExperienceLevels();
			ImportCiv3DefensiveBonuses();
			save.HealRates["friendly_field"] = 1;
			save.HealRates["neutral_field"] = 1;
			save.HealRates["hostile_field"] = 0;
			save.HealRates["city"] = 2;
			save.ScenarioSearchPath = biq?.Game[0].ScenarioSearchFolders;
			ImportBarbarianInfo();
			ImportCitizenTypes();
			ImportGovernments();
			ImportDifficulties();
			ImportRules();
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
			save.TurnNumber = savData.Game.TurnNumber;

			ImportSharedBiqData();
			ImportSavLeaders();
			ImportSavUnits();
			ImportSavCities();
			save.GameDifficulty = save.Difficulties[savData.Game.DifficultyID];

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
					continent = civ3Tile.Continent,
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
				if (civ3Tile.ResourceID != -1) {
					tile.resource = save.Resources[civ3Tile.ResourceID].Key;
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
					continent = civ3Tile.Continent,
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
				if (civ3Tile.Resource != -1) {
					tile.resource = save.Resources[civ3Tile.Resource].Key;
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

		private void ImportCiv3Resources() {
			GOOD[] Good = biq?.Good ?? defaultBiq.Good;
			foreach (GOOD good in Good) {
				Resource resource = new Resource {
					Key = good.Name,
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
				if (good.Prerequisite > -1) {
					resource.Prerequisite = save.Techs[good.Prerequisite].id;
				}

				save.Resources.Add(resource);
			}
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
					isBarbarian = i == 0 ? true : false,
				};
				foreach (RACE_City city in theBiq.RaceCityName[i]) {
					civ.cityNames.Add(city.Name);
				}
				civ.traits = LoadCivTraits(race).ToHashSet();

				// Look up the image for non-barbarian civs.
				string artName = pediaIcons.GetLeaderArtName(race.CivilopediaEntry);
				if (artName != null) {
					civ.leaderArtFile = artName;
				}

				save.Civilizations.Add(civ);
				i++;
			}
		}

		private static IEnumerable<Civilization.Trait> LoadCivTraits(RACE race) {
			return new[] {
				(race.Militaristic, Civilization.Trait.Militaristic),
				(race.Commercial, Civilization.Trait.Commercial),
				(race.Expansionist, Civilization.Trait.Expansionist),
				(race.Scientific, Civilization.Trait.Scientific),
				(race.Religious, Civilization.Trait.Religious),
				(race.Industrious, Civilization.Trait.Industrious),
				(race.Agricultural, Civilization.Trait.Agricultural),
				(race.Seafaring, Civilization.Trait.Seafaring),
			}
			.Where(t => t.Item1)
			.Select(t => t.Item2);
		}

		private void ImportBicLeaders() {
			BiqData theBiq = biq.Race is null ? defaultBiq : biq;

			Government defaultGovernment = save.Governments.Find(g => g.defaultType) ?? save.Governments[0];

			// Make a player for each civ. The barbarians are always civ 0.
			for (int i = 0; i < save.Civilizations.Count; ++i) {
				save.Players.Add(MakeSavePlayerFromCiv(save.Civilizations[i],
									   isBarbarian: i == 0,
									   isHuman: false,
									   era: ""));

				// Set a government for players not associated with LEAD.
				// Usually, this applies only to barbarians, but in some scenarios
				// it may also include civilizations that exist in the game files
				// but are not actually part of the gameplay.
				save.Players.Last().governmentId = defaultGovernment.id;
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

				player.governmentId = save.Governments[lead.Government].id;

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
			int currentTurn = save.TurnNumber;
			int i = 0;
			foreach (QueryCiv3.Sav.LEAD leader in savData.Lead) {
				if (leader.RaceID == -1) {
					continue; // can probably break here
				}
				Civilization civ = save.Civilizations[leader.RaceID];
				SavePlayer player = MakeSavePlayerFromCiv(civ,
										  isBarbarian: i == 0,
										  isHuman: i == 1,
										  era: theBiq.Eras[leader.Era].CivilopediaEntry);

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

				// Populate player's research queue
				for (int t = 0; t < savData.LeadTechQueue[i].Length; ++t) {
					int techItem = savData.LeadTechQueue[i][t];
					player.researchQueue.Add(save.Techs[techItem].id);
				}

				player.gold = leader.Gold;
				player.beakers = leader.Beakers;
				player.turnsResearched = leader.TurnsResearched;
				player.scienceRate = leader.ScienceRate;
				player.luxuryRate = leader.LuxuryRate;
				player.taxRate = leader.TaxRate;
				player.governmentId = save.Governments[leader.Government].id;
				player.inAnarchyUntilTurn = save.TurnNumber + leader.AnarchyTurnsLeft;

				save.Players.Add(player);
				i++;
			}

			// Now that we know all the players, fill in details about their
			// relationship to each other.
			i = 0;
			foreach (QueryCiv3.Sav.LEAD leader in savData.Lead) {
				List<int> contacts = leader.GetContact();
				List<bool> warStatus = leader.GetWarStatuses();
				List<int> refuseContactForTurns = leader.GetRefuseContactForTurns();
				for (int j = 0; j < contacts.Count; ++j) {
					if (contacts[j] > 0) {
						QueryCiv3.Sav.LEAD_LEAD relationship = savData.ReputationRelationship[i][j];
						save.Players[i].playerRelationships.Add(save.Players[j].id.ToString(), new PlayerRelationship() {
							atWar = warStatus[j],
							warDeclarationCount = relationship.WarDeclarationCount,
							wasSneakAttacked = relationship.WasSneakAttacked == 1,
							refuseContactUntilTurn =
								refuseContactForTurns[j] > 0 ?
									save.TurnNumber + refuseContactForTurns[j] : -1,
						});
					}
				}
				++i;
			}

			// Multi-Turn deals

			#region Multi-turn-deal-binary-documentation
			// Each player's diplomatic relationship with another player is represented as two arrays in a row.
			// Well, most of the time, more details on this are further down.
			// If the diplomacy array is empty, that either means the players are at war, or one/both the players are not in the game.
			// The data we get from a .SAV file, have all the (potential) diplomatic relationships between all 32 players.
			// So Player 1 has an array of relationships for players 0-31,
			// but also Player 31 has an array 0-31, even if they are not in the game.

			// As stated before, each relationship is represented by 2 arrays in a row.
			// So for example if Player1 is at peace with Player2, there will be 2 arrays inside the diplomacy array of both players
			// and because peace is the basis for any diplomatic agreement, these arrays are always 0 and 1.

			// Here are some more visual representations of the arrays, so it's easier to understand what's happening
			// Example Peace between Player1 and Player2
			//
			// Player1 LEAD_LEAD_Diplomacy[2] <- size of the diplomacy array is 2,
			// so it's safe to say only 1 deal is currently active,
			// and from what we know, it should always be peace

			// This should be the same for Player2's diplomacy array
			//
			// [0] =
			//   Data1 = 1          <-- "Gives" peace to Player2
			//   Data2 = 0          <-- This deal will end at turn 0 (still active, never at war)
			//   EntryType = -1
			// [1] =
			//   Data1 = 0          <-- Peace (DealSubtype)
			//   Data2 = 0
			//   EntryType = 0      <-- Diplomatic Agreement (DealType)

			// Notice that there is no reference to the other player, we know it's Player1 & Player2
			// because we are specifically looking at Player1's relationship with Player2.
			// Stating the obvious here, just making sure it's clear what kind of data is (and is not) present in these arrays.

			// Array #0
			// Array #0 holds information like, who gives (1) and who receives (0) (Data1),
			// number of remaining turns (Data2); All players in a normal game start at peace, so that value is 0.
			// If at some point in the future they go at war, and then make peace, this value will hold the turn the peace deal ends.
			// A value of future turn means a deal is active and will end at that turn in the future.
			// A value of current/past turn means the deal is still active, even beyond the usual 20 turns.
			// This is true for all other deals, but usually a Gold per Turn deal doesn't go beyond the 20 turns, it automatically resets,
			// but a right of passage deal for example sometimes doesn't.

			// Array #1
			// Array #1 holds the type of deal (DealType enum), so EntryType 0 means a diplomatic agreement, 1 is a military alliance, etc...
			// Data1 holds what I named, subtype (DealSubType enum). So if it's a diplomatic agreement and Data1 is 0, it means Peace, 1 is a Mutual Protection Pact, and so on.
			// In a resource per turn deal Data1 will hold the index of the resource being traded (e.x. 0 is Horses, 12 is Spices, etc)
			// In a military alliance deal Data1 will hold the index of the 3rd player, the player the alliance is against (same for trade embargo)
			// Data2 is used when for example there is a resource per turn deal, it holds the index of the resource being traded (e.x. 0 is Horses, 12 is Spices, etc)
			// In the case of a gold per turn deal, Data2 holds the gpt amount.
			// But there is a caveat here. In some cases, and I am not entirely sure when, but it seems to happen when there are multiple deals that happened on the same turn,
			// the second array (of the nth deal, not the 1st) is not present. So the information for when the deal ends for example, is taken from the previous deal in the diplomacy array.
			// That is why in the implementation below the dealStart is outside the main diplomacy for-loop check, it keeps a state for that type of situation.
			// Another instance of a deal missing parts, is for example, when Player1 gives Player2 5 gpt.
			// In the Player1's diplomacy info (the one giving), there will be two arrays as described above.
			// But in the Player2's info (the one receiving), the second array that holds what type of deal and how much gold is being traded can be missing.

			// Example Right of passage (current turn 25)
			// This should be the same for Player2's diplomacy array
			// Player1 LEAD_LEAD_Diplomacy[4]
			// Peace as said is mandatory for any diplomatic agreement
			// ---------------------- First Deal -------------------------------
			// [0] =
			//   Data1 = 1       <- "Gives" peace to Player2
			//   Data2 = 0       <- This deal will end at turn 0 (still active, never at war)
			//   EntryType = -1
			// [1] =
			//   Data1 = 0       <- Peace (DealSubtype)
			//   Data2 = 0
			//   EntryType = 0   <- Diplomatic Agreement (DealType)
			// ---------------------- Second Deal -------------------------------
			// [2] =
			//   Data1 = 1       <- "Gives" ROP to Player2
			//   Data2 = 40       <- This deal will end at turn 40 (it's been active for 5 turns)
			//   EntryType = -1
			// [3] =
			//   Data1 = 2       <- Right-of-passage (DealSubtype)
			//   Data2 = 0
			//   EntryType = 0   <- Diplomatic Agreement (DealType)


			// Example Trade luxury (current turn 25)
			// Player1 LEAD_LEAD_Diplomacy[4]
			// Peace as said is mandatory for any diplomatic agreement
			// ---------------------- First Deal -------------------------------
			// Peace placeholder, so I am not cluttering the screen, it's the same as above
			// ---------------------- Second Deal -------------------------------
			// [2] =
			//   Data1 = 1       <- "Gives" Spices to Player2
			//   Data2 = 40       <- This deal will end at turn 40 (it's been active for 5 turns)
			//   EntryType = -1
			// [3] =
			//   Data1 = 12      <- Luxury per turn (Spices)
			//   Data2 = 0
			//   EntryType = 6   <- Luxury (DealType)


			// Example Gold per turn with missing array for receiving player (current turn 25)
			// Player1 LEAD_LEAD_Diplomacy[4]
			// ---------------------- First Deal -------------------------------
			// Peace
			// ---------------------- Second Deal -------------------------------
			// [2] =
			//   Data1 = 1       <- "Gives" 3 gpt to Player2
			//   Data2 = 40       <- This deal will end at turn 40 (it's been active for 5 turns)
			//   EntryType = -1
			// [3] =
			//   Data1 = 0
			//   Data2 = 3       <- Gold per turn amount
			//   EntryType = 7   <- Gold (DealType)

			// in contrast, Player2's diplomacy array might look like this

			// Player2 LEAD_LEAD_Diplomacy[3] <-- 3 instead of 4!
			// ---------------------- First Deal -------------------------------
			// Peace
			// ---------------------- Second Deal -------------------------------
			// [2] =
			//   Data1 = 0       <- "Receives" 3 gpt from Player1
			//   Data2 = 40       <- This deal will end at turn 40 (it's been active for 5 turns)
			//   EntryType = -1
			// ---------------------- End ------------------------------
			//
			// Notice how Player2 is basically unaware that they receive 3 gpt from Player1,
			// they only know that they are receiving something up to turn 40
			#endregion

			i = 0;
			foreach (QueryCiv3.Sav.LEAD leader in savData.Lead) {
				int turnsRemaining = 0;
				int dealStart = 0;
				int defaultDealDuration = save.Rules.DefaultDealDuration;
				List<int> contacts = leader.GetContact();
				for (int j = 0; j < contacts.Count; ++j) {
					if (contacts[j] > 0) {
						// skip barbarians, they can't have any diplomatic relationships, they are at war against everyone, at all times
						if (i <= 0 || j <= 0) continue;

						// skip indexes of players that are not active
						if (j >= save.Players.Count || i >= save.Players.Count) continue;

						QueryCiv3.Sav.LEAD_LEAD_Diplomacy[] diplomacy = savData.LeadLeadDiplomacy[i, j];
						// a civ doesn't have any diplomatic relationships with itself, or with another civ they are at war with
						if (i == j || diplomacy.Length == 0) continue;

						for (int d = 0; d < diplomacy.Length; ++d) {
							DealType deadType = DealType.None;
							DealSubType dealSubType = DealSubType.None;
							DealDetails dealDetails = DealDetails.None;
							int goldPerTurn = 0;
							int resourceIndex = -1;
							string resourcePerTurn = null;
							ID againstPlayer = null;
							if (diplomacy[d].EntryType == -1) {
								turnsRemaining = diplomacy[d].Data2 - currentTurn > 0 ? diplomacy[d].Data2 - currentTurn : 0;
								dealStart = diplomacy[d].Data2 > 0 ? currentTurn - (defaultDealDuration - turnsRemaining) : 0;
								// I am pretty sure that is what these values represent, take it with a grain of salt.
								// We assign these manually anyway in each deal
								// but still could come handy at some point to know what they are.
								if (diplomacy[d].Data1 == 0) {
									dealDetails = DealDetails.Inbound;
								} else if (diplomacy[d].Data1 == 1) {
									dealDetails = DealDetails.Outbound;
								}
							}

							if (d + 1 <= diplomacy.Length - 1) {
								deadType = (DealType)diplomacy[d + 1].EntryType;
								if (deadType == DealType.DiplomaticAgreement) {
									if (diplomacy[d + 1].Data1 == 0) {
										dealSubType = DealSubType.Peace;
									} else if (diplomacy[d + 1].Data1 == 1) {
										dealSubType = DealSubType.MutualProtectionPact;
									} else if (diplomacy[d + 1].Data1 == 2) {
										dealSubType = DealSubType.RightOfPassage;
									}

									dealDetails = DealDetails.Exchange;
								} else if (deadType == DealType.Alliance) {
									dealSubType = DealSubType.MilitaryAlliance;
									againstPlayer = save.Players[diplomacy[d + 1].Data1].id;
									dealDetails = DealDetails.Exchange;
								} else if (deadType == DealType.Embargo) {
									dealSubType = DealSubType.TradeEmbargo;
									againstPlayer = save.Players[diplomacy[d + 1].Data1].id;
									dealDetails = DealDetails.Exchange;
								}
								  // These types of deals are only present in full on the civ that gives the gold/resources/luxuries
								  else if (deadType == DealType.Gold) {
									dealSubType = DealSubType.GoldPerTurn;
									goldPerTurn = diplomacy[d + 1].Data2;
									dealDetails = DealDetails.Outbound;
								} else if (deadType == DealType.Luxury) {
									dealSubType = DealSubType.LuxuryPerTurn;
									resourceIndex = diplomacy[d + 1].Data1;
									resourcePerTurn = save.Resources[resourceIndex].Key;
									dealDetails = DealDetails.Outbound;
								} else if (deadType == DealType.Resource) {
									dealSubType = DealSubType.ResourcePerTurn;
									resourceIndex = diplomacy[d + 1].Data1;
									resourcePerTurn = save.Resources[resourceIndex].Key;
									dealDetails = DealDetails.Outbound;
								} else {
									continue;
								}

								if (dealSubType != DealSubType.None
									&& save.Players[i].playerRelationships.TryGetValue(save.Players[j].id.ToString(), out PlayerRelationship pr)) {
									MultiTurnDeal mtd = new MultiTurnDeal(deadType, dealSubType, dealDetails, goldPerTurn, resourcePerTurn, defaultDealDuration, dealStart, againstPlayer);
									pr.multiTurnDeals.Add(mtd);
								}

								// Add inbound info to other player because that info is not exactly present in the binary
								if (dealDetails == DealDetails.Outbound
									&& dealSubType != DealSubType.None
									&& save.Players[j].playerRelationships.TryGetValue(save.Players[i].id.ToString(), out PlayerRelationship prOther)) {
									MultiTurnDeal mtd = new MultiTurnDeal(deadType, dealSubType, DealDetails.Inbound, goldPerTurn, resourcePerTurn, defaultDealDuration, dealStart, againstPlayer);
									prOther.multiTurnDeals.Add(mtd);
								}
							}
						}
					}
				}
				++i;
			}

			foreach (SavePlayer savePlayer in save.Players) {
				int other = 0;
				log.Information($"- - - - - - - - - - - - - - - - - - {savePlayer.civilization} - - - - - - - - - - - - - - - - - - ");
				foreach (PlayerRelationship pr in savePlayer.playerRelationships.Values) {
					other++;
					foreach (MultiTurnDeal mtd in pr.multiTurnDeals) {
						string against = mtd.dealSubType == DealSubType.MilitaryAlliance || mtd.dealSubType == DealSubType.TradeEmbargo ? $"(against: {save.Players.First(p => p.id == mtd.againstPlayer).civilization}({mtd.againstPlayer}))" : "";
						string gpt = mtd.dealSubType == DealSubType.GoldPerTurn? $"({mtd.goldPerTurn} gold per turn)" : "";
						string rpt = mtd.dealSubType == DealSubType.LuxuryPerTurn || mtd.dealSubType == DealSubType.ResourcePerTurn ? $"({mtd.resourcePerTurn} per turn)" : "";
						log.Information($"{savePlayer.civilization}({savePlayer.id}) " +
										$"has {mtd.dealSubType} " +
										$"with {save.Players[other].civilization}({save.Players[other].id}) " +
										$"for another {mtd.TurnsRemaining(save.TurnNumber)} turns {mtd.turnStartDeal}-{mtd.turnEndDeal}" +
										$" {against} {gpt} {rpt} {mtd.dealDetails}");
					}
				}
			}
		}

		private SavePlayer MakeSavePlayerFromCiv(Civilization civ, bool isBarbarian, bool isHuman, string era) {
			return new SavePlayer {
				id = ids.CreateID("player"),
				colorIndex = civ.colorIndex,
				human = isHuman,
				civilization = civ.name,

				// Never let barbarians play before a real player.
				hasPlayedCurrentTurn = isBarbarian,

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
					WorkerProgressTowardsJob = unit.WorkerProgressTowardsJob,
					WorkerJob = (unit.WorkerJob==-1) ? null: save.TerraForms[unit.WorkerJob].Id,
					isAutomated = unit.IsAutomated,
				};
				if (unit.Fortified) {
					saveUnit.action = "fortified";
				}

				save.Units.Add(saveUnit);
			}
		}

		private void ImportBicUnits() {
			BiqData theBiq = biq.Unit is null ? defaultBiq : biq;

			var createUnitAtLocation = (SavePlayer player, int unitType, string experienceLevel, int hitPoints, int x, int y) => {
				PRTO prototype = theBiq.Prto[unitType];
				SaveUnit saveUnit = new SaveUnit{
					id = ids.CreateID(prototype.Name),
					owner = player.id,
					prototype = prototype.Name,
					currentLocation = new TileLocation(x, y),
					previousLocation = new TileLocation(x, y),
					experience = experienceLevel,
					hitPointsRemaining = hitPoints, // TODO: include bonus hitpoints from unit type
					movePointsRemaining = (float)prototype.Movement,
				};
				return saveUnit;
			};

			foreach (UNIT unit in theBiq.Unit) {
				if (unit.Owner >= save.Players.Count) {
					continue;
				}

				// The owner index is into the list of civs, and we have a 1:1
				// mapping of players and civs.
				SavePlayer player = save.Players[unit.Owner];
				ExperienceLevel experience = save.ExperienceLevels[unit.ExperienceLevel];
				save.Units.Add(createUnitAtLocation(player, unit.UnitType, experience.key, experience.baseHitPoints, unit.X, unit.Y));
			}

			RULE rule = theBiq.Rule[0];
			foreach (SLOC starting_location in theBiq.Sloc) {
				// Skip barbarians
				if (starting_location.OwnerType <= 1) {
					continue;
				}

				// The owner index is into the list of civs, and we have a 1:1
				// mapping of players and civs.
				SavePlayer player = save.Players[starting_location.Owner];
				int baseHitPoints = 3;
				if (rule.StartUnitType1 >= 0) {
					save.Units.Add(createUnitAtLocation(player, rule.StartUnitType1, "Regular", baseHitPoints, starting_location.X, starting_location.Y));
				}
				if (rule.StartUnitType2 >= 0) {
					save.Units.Add(createUnitAtLocation(player, rule.StartUnitType2, "Regular", baseHitPoints, starting_location.X, starting_location.Y));
				}
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
					buildings = ImportCityBuildingsFromSav(i),
					turnsOfUnhappinessDueToPopRushing = city.TurnsOfUnhappinessDueToPopRushing,
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

		List<SaveCityBuilding> ImportCityBuildingsFromSav(int cityIndex) {
			List<SaveCityBuilding> res = [];

			var city = savData.City[cityIndex];
			QueryCiv3.Sav.CITY_Building[] cityBuildings = savData.CityBuilding[cityIndex];

			BiqData theBiq = biq.Bldg is null ? defaultBiq : biq;

			for (int buildingIndex = 0; buildingIndex < cityBuildings.Length; ++buildingIndex) {
				var building = cityBuildings[buildingIndex];

				if (building.BuiltByPlayer != -1 && city.Bitm.IsBuildingUsable(buildingIndex)) {
					res.Add(new SaveCityBuilding {
						building = theBiq.Bldg[buildingIndex].Name,
						builtByPlayer = save.Players[building.BuiltByPlayer].id,
						year = building.Year,
						totalCulture = building.Culture,
					});

					if (theBiq.Bldg[buildingIndex].Wonder) {
						save.GreatWondersBuilt.Add(theBiq.Bldg[buildingIndex].Name);
					}
				}
			}

			return res;
		}

		List<SaveCityBuilding> ImportCityBuildingsFromBiq(int cityIndex, ID player) {
			BiqData theBiq = biq.City is null ? defaultBiq : biq;
			List<SaveCityBuilding> res = [];
			int[] cityBuildings = theBiq.CityBuilding[cityIndex];

			for (int buildingIndex = 0; buildingIndex < cityBuildings.Length; ++buildingIndex) {
				BLDG building = theBiq.Bldg[cityBuildings[buildingIndex]];

				res.Add(new SaveCityBuilding {
					building = building.Name,
					builtByPlayer = player,
					year = 0,
					totalCulture = 0,
				});

				if (building.Wonder) {
					save.GreatWondersBuilt.Add(building.Name);
				}
			}

			return res;
		}

		private (string, ProducibleType) CityToProducible(QueryCiv3.Sav.CITY city) {
			PRTO[] unitPrototypes = biq.Prto ?? defaultBiq.Prto;
			BiqData theBiq = biq.Bldg is null ? defaultBiq : biq;

			return city.ConstructingType switch {
				0 => ("Worker", ProducibleType.UNIT), // TODO: Wealth production is not implemented yet
				1 => (theBiq.Bldg[city.Constructing].Name, ProducibleType.BUILDING),
				2 => (unitPrototypes[city.Constructing].Name, ProducibleType.UNIT),
				_ => throw new NotImplementedException()
			};
		}

		private void ImportBicCities() {
			BiqData theBiq = biq.City is null ? defaultBiq : biq;

			for (int cityIndex = 0; cityIndex < theBiq.City.Length; ++cityIndex) {
				CITY city = theBiq.City[cityIndex];

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
					buildings = ImportCityBuildingsFromBiq(cityIndex, player.id),
					shieldsStored = 0,
					foodStored = 0,
				};
				saveCity.perPlayerCulture.Add(player.id.ToString(), city.Culture);

				save.Cities.Add(saveCity);
			}
		}

		private static IEnumerable<UnitAction> GetUnitActions(PRTO prto) {
			if (prto.BuildCity) yield return UnitAction.BuildCity;
			if (prto.Bombard) yield return UnitAction.Bombard;
			if (prto.SkipTurn) yield return UnitAction.Hold;
			if (prto.Wait) yield return UnitAction.Wait;
			if (prto.Fortify) yield return UnitAction.Fortify;
			if (prto.Disband) yield return UnitAction.Disband;
			if (prto.GoTo) yield return UnitAction.Goto;
			if (prto.Explore) yield return UnitAction.Explore;
			if (prto.Automate) yield return UnitAction.Automate;
		}

		private static IEnumerable<TerraformKey> GetUnitTerraforms(PRTO prto) {
			if (prto.BuildRoad) yield return TerraformKey.BuildRoad;
			if (prto.BuildRailroad) yield return TerraformKey.BuildRailroad;
			if (prto.BuildMine) yield return TerraformKey.BuildMine;
			if (prto.Irrigate) yield return TerraformKey.Irrigate;
			if (prto.ClearJungle) yield return TerraformKey.ClearWetlands;
			if (prto.ClearForest) yield return TerraformKey.ClearForest;
			if (prto.BuildBarricade) yield return TerraformKey.BuildBarricade;
			if (prto.BuildFortress) yield return TerraformKey.BuildFortress;
		}

		private SaveUnitPrototype.Unique ImportUniqueUnitData(PRTO prto) {
			int civIndex = prto.AvailableTo.GetUniqueCivIndex();

			if (civIndex == -1) {
				return null;
			}

			return new() { civilization = save.Civilizations[civIndex].name };
		}

		private static bool IsUnproducible(PRTO prto) {
			int[] availableTo = prto.AvailableTo.GetAvailableCivIndexes().ToArray();

			// TODO: Implement proper logic for Army production
			return availableTo.Length == 0 || prto.ShieldCost < 1 || prto.Army;
		}

		private void ImportUnitPrototypes() {
			PRTO[] Prto = biq.Prto ?? defaultBiq.Prto;
			foreach (PRTO prto in Prto) {
				SaveUnitPrototype prototype = new();
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
				prototype.actions.UnionWith(GetUnitActions(prto));
				prototype.terraformActions.UnionWith(GetUnitTerraforms(prto).Select(tfKey => terraformIdByCiv3Key[tfKey]));

				prototype.unique = ImportUniqueUnitData(prto);
				prototype.unproducible = IsUnproducible(prto);

				if (prto.Required != -1) {
					prototype.requiredTech = save.Techs[prto.Required].id;
				}

				if (prto.RequiredResource1 != -1) {
					prototype.requiredResources.Add(save.Resources[prto.RequiredResource1].Key);
				}

				if (prto.RequiredResource2 != -1) {
					prototype.requiredResources.Add(save.Resources[prto.RequiredResource2].Key);
				}

				//Temporary check until #330 is finished
				if (!save.UnitPrototypes.Where(p => p.name == prototype.name).Any()) {
					save.UnitPrototypes.Add(prototype);
				}
			}
		}

		// This method assigns standard units that are replaced by unique units.
		//
		// A unique unit replaces a standard unit if both share the same tech requirement
		// and the standard unit is unproducible by the civilization to which the unique unit belongs.
		//
		// For example, this method updates the Mounted Warrior prototype to indicate that it replaces the Horseman.
		private void ImportUniqueUnitReplacements() {
			var unitPrototypeDict = save.UnitPrototypes.ToDictionary(b => b.name);

			// Group unique units by civilization.
			// In the base ruleset a civilization only has one unique unit,
			// but this may vary in scenarios.
			var uniqueUnitPrototypesByCiv = save.UnitPrototypes
					.Where(u => u.unique != null)
					.ToLookup(u => u.unique.civilization);

			PRTO[] Prto = biq.Prto ?? defaultBiq.Prto;

			foreach (PRTO standardUnitPrto in Prto) {
				string standardUnitName = standardUnitPrto.Name;
				SaveUnitPrototype standardUnit = unitPrototypeDict[standardUnitName];

				// Skip units that are either unique or unproducible (cannot be built normally)
				if (standardUnit.unique != null) {
					continue;
				}

				if (standardUnit.unproducible) {
					continue;
				}

				// For each civilization that cannot build the standard unit
				foreach (int civIndex in standardUnitPrto.AvailableTo.GetUnavailableCivIndexes()) {
					if (civIndex >= save.Civilizations.Count) {
						break;
					}

					var uniqueUnits = uniqueUnitPrototypesByCiv[save.Civilizations[civIndex].name];

					foreach (SaveUnitPrototype uniqueUnit in uniqueUnits) {
						// If the unique unit has the same tech requirement as the standard unit,
						// mark the unique unit as a replacement for the standard unit
						if (uniqueUnit.requiredTech == standardUnit.requiredTech) {
							uniqueUnit.unique.replace = standardUnitName;
						}
					}
				}
			}
		}

		// This method loads unit upgrades from CIV3 data. In CIV3, unique units are part of the upgrade chain.
		//
		// For example, the upgrade path for Horseman looks like this:
		// Horseman->Mounted Warrior->Three-Man Chariot->Knight->Keshik->Ansar Warrior->Rider->Samurai->War Elephant->Cavalry.
		// see also: https://forums.civfanatics.com/threads/how-to-upgrade-regular-units-to-uus.108396/
		//
		// When loading this data, the method ignores the unique units in the upgrade chain.
		// Instead, each unit of the chain will be assigned an upgrade that represents the closest non-unique unit
		// that also requires a tech advancement over the base unit.
		//
		// For example, this method will mark that Horseman upgrades to Knight and that Keshik upgrades to Cavalry.
		private void ImportUnitUpgrades() {
			Dictionary<SaveUnitPrototype, SaveUnitPrototype> upgradeDict = BuildUpgradeDict();

			foreach (SaveUnitPrototype proto in save.UnitPrototypes) {
				proto.upgradeTo = GetUnitUpgrade(proto, upgradeDict);
			}
		}

		// This method builds a Dictionary of unit upgrades based on the CIV3 data.
		// The dictionary represents the raw upgrade relationships as defined in the game files.
		// The dictionary serves as an intermediate data structure for the ImportUnitUpgrades process,
		// before filtering out unique units.
		private Dictionary<SaveUnitPrototype, SaveUnitPrototype> BuildUpgradeDict() {
			PRTO[] Prto = biq.Prto ?? defaultBiq.Prto;
			var unitPrototypeDict = save.UnitPrototypes.ToDictionary(b => b.name);

			Dictionary<SaveUnitPrototype, SaveUnitPrototype> upgradeDict = [];

			foreach (PRTO prto in Prto) {
				SaveUnitPrototype upgradeFrom = unitPrototypeDict[prto.Name];
				if (prto.UpgradeTo != -1) {
					SaveUnitPrototype upgradeTo = unitPrototypeDict[Prto[prto.UpgradeTo].Name];
					upgradeDict[upgradeFrom] = upgradeTo;
				} else {
					upgradeDict[upgradeFrom] = null;
				}
			}

			return upgradeDict;
		}

		// This method returns the name of the first valid unit upgrade in the upgrade chain.
		// A valid upgrade must require a different technology than the base unit and must not be a unique unit.
		// If no valid upgrade is found, it returns null.
		private static string GetUnitUpgrade(SaveUnitPrototype proto, Dictionary<SaveUnitPrototype, SaveUnitPrototype> upgradeDict) {
			SaveUnitPrototype currentProto = proto;

			while (true) {
				// Check if there's an upgrade available
				var upgrade = upgradeDict[currentProto];
				if (upgrade == null) {
					return null;
				}

				// If this upgrade represents a technology advancement over the base unit and is not a unique unit, return it
				if (upgrade.requiredTech != proto.requiredTech && upgrade.unique == null) {
					return upgrade.name;
				}

				// Otherwise, continue checking the upgrade chain
				currentProto = upgrade;
			}
		}

		private void ImportBuildings() {
			BLDG[] Bldg = biq.Bldg ?? defaultBiq.Bldg;

			foreach (BLDG bldg in Bldg) {
				if (bldg.Name == "Wealth") {
					continue; // We don't consider Wealth as a building
				}

				SaveBuilding building = new() {
					name=bldg.Name,
					shieldCost=bldg.Cost * 10, // In Civ3 files, building costs are stored at 1/10th of their actual value
					populationCost=0, // In Civ3, a building cannot have a population cost
					isSmallWonder=bldg.SmallWonder,
					greatWonderProperties=bldg.Wonder ? new SaveBuilding.GreatWonderProperties() : null,
					culturePerTurn=bldg.Culture,
					contentFacesInCity=bldg.ContentFaces - bldg.UnhappyFaces,
					iconRowIndex=pediaIcons.buildingToRowNumberMapping[bldg.CivilopediaEntry],
					combatDefenseBonus=bldg.DefenseBonus / 100.0,
					maintenanceCost=bldg.MaintenanceCost,
				};

				if (bldg.RequiredAdvance != -1) {
					building.requiredTech = save.Techs[bldg.RequiredAdvance].id;
				}

				if (bldg.RequiredBuilding != -1) {
					building.requiredBuilding = Bldg[bldg.RequiredBuilding].Name;
				}

				if (bldg.RequiredResource1 != -1) {
					building.requiredResources.Add(save.Resources[bldg.RequiredResource1].Key);
				}

				if (bldg.RequiredResource2 != -1) {
					building.requiredResources.Add(save.Resources[bldg.RequiredResource2].Key);
				}

				if (bldg.RenderedObsoleteBy != -1) {
					building.renderedObsoleteBy = save.Techs[bldg.RenderedObsoleteBy].id;
				}

				if (bldg.GainInEveryCity >= 0) {
					building.greatWonderProperties.buildingGainedInEveryCity = Bldg[bldg.GainInEveryCity].Name;
				}
				if (bldg.GainInEveryCityOnContinent >= 0) {
					building.greatWonderProperties.buildingGainedInEveryCityOnContinent = Bldg[bldg.GainInEveryCityOnContinent].Name;
				}

				building.flags = LoadBuildingFlags(bldg).ToHashSet();
				building.traits = LoadBuildingTraits(bldg).ToHashSet();

				// Buildings with bombard defense are treated as walls in civ3.
				if (bldg.BombardDefense > 0) {
					building.flags.Add(SaveBuilding.Flag.ProvidesWalls);
					building.flags.Add(SaveBuilding.Flag.CanOnlyBeBuiltInTowns);
				}

				MapFlagsToLuaFunctions(building);

				save.Buildings.Add(building);
			}
		}

		private static IEnumerable<SaveBuilding.Flag> LoadBuildingFlags(BLDG bldg) {
			return new[] {
				(bldg.CenterOfEmpire, SaveBuilding.Flag.IsCenterOfEmpire),
				(bldg.CoastalInstallation, SaveBuilding.Flag.MustBeCoastal),
				(bldg.MustBeNearRiver, SaveBuilding.Flag.MustBeNearRiver),
				(bldg.VeteranGroundUnits, SaveBuilding.Flag.VeteranGroundUnits),
				(bldg.VeteranSeaUnits, SaveBuilding.Flag.VeteranSeaUnits),
				(bldg.IncreasesLuxuryTrade, SaveBuilding.Flag.IncreasesLuxuryTrade),
				(bldg.ReducesCorruption, SaveBuilding.Flag.ReducesCorruption),
				(bldg.ForbiddenPalace, SaveBuilding.Flag.ForbiddenPalace),
				(bldg.IncreasesShieldsInWater, SaveBuilding.Flag.IncreasesShieldsInWater),
				(bldg.IncreasesFoodInWater, SaveBuilding.Flag.IncreasesFoodInWater),
				(bldg.IncreasesTradeInWater, SaveBuilding.Flag.IncreasesTradeInWater),
				(bldg.AllowsCitySize2, SaveBuilding.Flag.AllowsCitySize2),
				(bldg.AllowsCitySize3, SaveBuilding.Flag.AllowsCitySize3),
				(bldg.DoublesCityGrowthRate, SaveBuilding.Flag.DoublesCityGrowthRate),
			}
			.Where(t => t.Item1)
			.Select(t => t.Item2);
		}

		private static IEnumerable<Civilization.Trait> LoadBuildingTraits(BLDG bldg) {
			return new[] {
				(bldg.Militaristic, Civilization.Trait.Militaristic),
				(bldg.Commercial, Civilization.Trait.Commercial),
				(bldg.Expansionist, Civilization.Trait.Expansionist),
				(bldg.Scientific, Civilization.Trait.Scientific),
				(bldg.Religious, Civilization.Trait.Religious),
				(bldg.Industrious, Civilization.Trait.Industrious),
				(bldg.Agricultural, Civilization.Trait.Agricultural),
				(bldg.Seafaring, Civilization.Trait.Seafaring),
			}
			.Where(t => t.Item1)
			.Select(t => t.Item2);
		}

		private static void MapFlagsToLuaFunctions(SaveBuilding building) {
			var flagToProductionRule = new Dictionary<SaveBuilding.Flag, string>
			{
				{ SaveBuilding.Flag.MustBeCoastal, "must_be_coastal" },
				{ SaveBuilding.Flag.MustBeNearRiver, "must_be_near_river" },
				{ SaveBuilding.Flag.AllowsCitySize2, "allows_city_size_2" },
				{ SaveBuilding.Flag.AllowsCitySize3, "allows_city_size_3" },
				{ SaveBuilding.Flag.CanOnlyBeBuiltInTowns, "can_only_be_built_in_towns"}
			};

			var flagToUnitProductionEffect = new Dictionary<SaveBuilding.Flag, string>
			{
				{ SaveBuilding.Flag.VeteranGroundUnits, "veteran_ground_units" },
				{ SaveBuilding.Flag.VeteranSeaUnits, "veteran_sea_units" },
			};

			var flagToTileModifier = new Dictionary<SaveBuilding.Flag, string>
			{
				{ SaveBuilding.Flag.IncreasesFoodInWater, "increases_food_in_water" },
				{ SaveBuilding.Flag.IncreasesShieldsInWater, "increases_shields_in_water" },
				{ SaveBuilding.Flag.IncreasesTradeInWater, "increases_trade_in_water" },
			};

			foreach (var flag in building.flags) {
				if (flagToProductionRule.TryGetValue(flag, out var productionRule)) {
					building.productionPrerequisites.Add($"buildings.production_rules.{productionRule}");
				}

				if (flagToUnitProductionEffect.TryGetValue(flag, out var unitEffect)) {
					building.onFinishedUnitProduction.Add($"buildings.unit_production_effects.{unitEffect}");
				}

				if (flagToTileModifier.TryGetValue(flag, out var tileModifier)) {
					building.tileModifiers.Add($"buildings.tile_modifiers.{tileModifier}");
				}
			}
		}

		private void ImportCiv3TerrainTypes() {
			TERR[] Terr = biq.Terr ?? defaultBiq.Terr;
			bool[,] TerrGood = biq.TerrGood ?? defaultBiq.TerrGood;
			int civ3Index = 0;
			foreach (TERR terrain in Terr) {
				TerrainType c7TerrainType = TerrainType.ImportFromCiv3(civ3Index, terrain);
				for (int i = 0; i < TerrGood.GetLength(1); ++i) {
					if (TerrGood[civ3Index, i]) {
						c7TerrainType.allowedResources.Add(save.Resources[i].Key);
					}
				}
				AddYieldBonusesForTerrainImprovements(c7TerrainType.Key, terrain);
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

		private SaveUnitPrototype MatchCiv3IndexToImportedUnit(int civ3Index) {
			PRTO[] Prto = biq.Prto ?? defaultBiq.Prto;
			if (civ3Index == -1) return null;
			return save.UnitPrototypes.Where(up => up.name == Prto[civ3Index].Name).First();
		}

		private void ImportBarbarianInfo() {
			RULE Rule = biq?.Rule?[0] ?? defaultBiq.Rule[0];
			PRTO[] Prto = biq?.Prto ?? defaultBiq.Prto;
			BarbarianInfo barbInfo = save.BarbarianInfo;

			barbInfo.basicBarbarianUnit = MatchCiv3IndexToImportedUnit(Rule.BasicBarbarianUnitType)?.name;
			barbInfo.advancedBarbarianUnit = MatchCiv3IndexToImportedUnit(Rule.AdvancedBarbarianUnitType)?.name;
			barbInfo.barbarianSeaUnit = MatchCiv3IndexToImportedUnit(Rule.BarbarianSeaUnitType)?.name;
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
					RequiredForEraAdvancement = !t.NotRequiredForEraAdvancement,
					EraCivilopediaName = t.Era == -1 ? "Hidden" : theBiq.Eras[t.Era].CivilopediaEntry,
					SmallIconPath = t.Era == -1 ? "" : pediaIcons.GetTechIconPath(t.CivilopediaEntry),
					X = t.X,
					Y = t.Y,
					flags = LoadTechFlags(t).ToHashSet(),
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

		private static IEnumerable<SaveTech.Flag> LoadTechFlags(TECH t) {
			return new[] {
				(t.BonusTechToFirstCivThatResearches, SaveTech.Flag.BonusTechToFirstCivThatResearches),
				(t.EnablesBridges, SaveTech.Flag.EnablesBridges),
			}
			.Where(t => t.Item1)
			.Select(t => t.Item2);
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

		private void ImportTerraforms() {
			BiqData theBiq = biq.Tfrm is null ? defaultBiq : biq;

			for (int i = 0; i < theBiq.Tfrm.Length; ++i) {
				TFRM t = theBiq.Tfrm[i];

				TerraformKey tfKey = ConvertCiv3Order(t.Order);

				SaveTerraform tf = new() {
					Id = ids.CreateID("Terraform"),
					Name = t.Name,
					CivilopediaEntry = t.CivilopediaEntry,
					TurnsToComplete = t.TurnsToComplete,
				};

				tf.SetUpByTerraformKey(tfKey);

				if (t.Required > -1) {
					tf.RequiredTech = save.Techs[t.Required].id;
				}
				if (t.RequiredResource1 > -1) {
					tf.RequiredResources.Add(save.Resources[t.RequiredResource1].Key);
				}
				if (t.RequiredResource2 > -1) {
					tf.RequiredResources.Add(save.Resources[t.RequiredResource2].Key);
				}

				save.TerraForms.Add(tf);
				terraformIdByCiv3Key[tfKey] = tf.Id;
			}
		}

		private void AddYieldBonusesForTerrainImprovements(string terrainKey, TERR terrainType) {
			void SetBonus(string improvementKey, Tile.YieldType type, int bonus) {
				SaveTerrainImprovement improvement = save.TerrainImprovements.Find(ti => ti.key == improvementKey);

				if (!improvement.bonusYields.TryGetValue(terrainKey, out var yieldDict)) {
					yieldDict = new Dictionary<Tile.YieldType, int>();
					improvement.bonusYields[terrainKey] = yieldDict;
				}

				yieldDict[type] = bonus;
			}

			if (terrainType.MiningBonus > 0) {
				SetBonus("mine", Tile.YieldType.Production, terrainType.MiningBonus);
			}
			if (terrainType.IrrigationBonus > 0) {
				SetBonus("irrigation", Tile.YieldType.Food, terrainType.IrrigationBonus);
			}
			if (terrainType.RoadBonus > 0) {
				SetBonus("road", Tile.YieldType.Commerce, terrainType.RoadBonus);
				SetBonus("railroad", Tile.YieldType.Commerce, terrainType.RoadBonus);
			}
		}

		private static TerraformKey ConvertCiv3Order(string order) {
			return order switch {
				"Build Mine" => TerraformKey.BuildMine,
				"Irrigate" => TerraformKey.Irrigate,
				"Build Fortress" => TerraformKey.BuildFortress,
				"Build Road" => TerraformKey.BuildRoad,
				"Build Railroad" => TerraformKey.BuildRailroad,
				"Plant Forest" => TerraformKey.PlantForest,
				"Clear Forest" => TerraformKey.ClearForest,
				"Clear Wetlands" => TerraformKey.ClearWetlands,
				"Clear Damage" => TerraformKey.ClearDamage,
				"Build Airfield" => TerraformKey.BuildAirfield,
				"Build Radar Tower" => TerraformKey.BuildRadarTower,
				"Build Outpost" => TerraformKey.BuildOutpost,
				"Build Barricade" or "Build Barricades" => TerraformKey.BuildBarricade,
				_ => throw new NotSupportedException($"Unknown order: {order}"),
			};
		}

		private void ImportGovernments() {
			BiqData theBiq = biq.Govt is null ? defaultBiq : biq;

			foreach (QueryCiv3.Biq.GOVT govt in theBiq.Govt) {
				Government g = new();
				g.id = ids.CreateID("Government");
				g.name = govt.Name;
				g.civilopediaEntry = govt.CivilopediaEntry;
				if (govt.PrerequisiteTechnology != -1) {
					g.prerequisiteTech = save.Techs[govt.PrerequisiteTechnology].id;
				}
				g.defaultType = govt.DefaultType == 1;
				g.transitionType = govt.TransitionType == 1;
				g.hasTilePenalty = govt.TilePenalty == 1;
				g.hasTradeBonus = govt.TradeBonus == 1;
				g.corruptionType = (Government.CorruptionType)govt.Corruption;
				g.hurryingType = (Government.HurryProductionType)govt.Hurrying;
				g.draftLimit = govt.DraftLimit;
				g.militaryPoliceLimit = govt.MilitaryPoliceLimit;
				g.workerRate = govt.WorkerRate;
				g.allUnitsFree = govt.FreeUnits == -1;
				g.freeUnitsPerTown = govt.FreeUnitsPerTown;
				g.freeUnitsPerCity = govt.FreeUnitsPerCity;
				g.freeUnitsPerMetropolis = govt.FreeUnitsPerMetropolis;
				g.unitCost = govt.UnitCost;

				save.Governments.Add(g);
			}
		}

		private void ImportDifficulties() {
			BiqData theBiq = biq.Diff is null ? defaultBiq : biq;

			foreach (QueryCiv3.Biq.DIFF diff in theBiq.Diff) {
				Difficulty d = new();
				d.id = ids.CreateID("Difficulty");
				d.Name = diff.Name;
				d.NumberOfCitizensBornContent = diff.NumberOfCitizensBornContent;
				d.MaxAiGovernmentTransitionTime = diff.MaxGovernmentTransitionTime;
				d.NumberOfAIDefensiveStartingUnits = diff.NumberOfAIDefensiveStartingUnits;
				d.NumberOfAIOffensiveStartingUnits = diff.NumberOfAIOffensiveStartingUnits;
				d.ExtraStartUnit1 = diff.ExtraStartUnit1;
				d.ExtraStartUnit2 = diff.ExtraStartUnit2;
				d.AdditionalFreeUnitSupport = diff.AdditionalFreeSupport;
				d.UnitSupportBonusForEachSettlement = diff.UnitSupportBonusForEachSettlement;
				d.AttackBonusAgainstBarbarians = diff.AttackBonusAgainstBarbarians;
				d.AiCostFactor = diff.CostFactor;
				d.PercentageOfOptimalCities = diff.PercentageOfOptimalCities;
				d.AIToAITradeRate = diff.AIToAITradeRate;
				d.CorruptionPercentage = diff.CorruptionPercentage;
				d.MilitaryLaw = diff.MilitaryLaw;

				save.Difficulties.Add(d);
			}
		}

		private void ImportRules() {
			BiqData theBiq = biq.Rule is null ? defaultBiq : biq;
			RULE rule = theBiq.Rule[0];

			save.Rules.MaximumResearchTime = rule.MaximumResearchTime;
			save.Rules.MinimumResearchTime = rule.MinimumResearchTime;
			save.Rules.MaximumLevel1CitySize = rule.MaximumLevel1CitySize;
			save.Rules.MaximumLevel2CitySize = rule.MaximumLevel2CitySize;
			save.Rules.ShieldValueInGold = rule.ShieldValueInGold;
			save.Rules.ForestValueInShields = rule.ForestValueInShields;
			save.Rules.CitizenValueInShields = rule.CitizenValueInShields;
			save.Rules.TurnPenaltyForEachHurrySacrifice = rule.TurnPenaltyForEachHurrySacrifice;
			save.GameDifficulty = save.Difficulties[rule.DefaultDifficultyLevel];
			if (rule.StartUnitType1 >= 0) {
				save.Rules.StartUnitType1 = theBiq.Prto[rule.StartUnitType1].Name;
			}
			if (rule.StartUnitType2 >= 0) {
				save.Rules.StartUnitType2 = theBiq.Prto[rule.StartUnitType2].Name;
			}
			if (rule.Scout >= 0) {
				save.Rules.ScoutUnitType = theBiq.Prto[rule.Scout].Name;
			}
			save.Rules.MaxRankOfWorkableTiles = 2;
			save.Rules.MaxRankOfBarbarianCampTiles = 2;
			save.Rules.DefaultDealDuration = 20;
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
				save.Map.techRate = civ3Save.Bic.Wsiz[civ3Save.Wrld.WsizID].TechRate;
				save.Map.optimalNumberOfCities = civ3Save.Bic.Wsiz[civ3Save.Wrld.WsizID].OptimalNumberOfCities;
			}
		}

		private static void SetMapDimensions(BiqData biq, SaveGame save) {
			if (biq is not null && biq.Wmap is not null && biq.Wmap.Length > 0) {
				save.Map.tilesTall = biq.Wmap[0].Height;
				save.Map.tilesWide = biq.Wmap[0].Width;
				save.Map.techRate = biq.Wsiz[biq.Wchr[0].WorldSize].TechRate;
				save.Map.optimalNumberOfCities = biq.Wsiz[biq.Wchr[0].WorldSize].OptimalNumberOfCities;
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

		// A handy utility for trying to reverse engineer various structs when
		// comparing SAV files.
		private void DumpObject(string label, object o) {
			Console.WriteLine(label);
			foreach (System.Reflection.PropertyInfo propertyInfo in o.GetType().GetProperties(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance)) {
				Console.WriteLine($"\t{propertyInfo.Name}={propertyInfo.GetValue(o)}");
			}
			foreach (System.Reflection.MethodInfo method in o.GetType().GetMethods(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance)) {
				// Print the common case of methods returning lists with no argments
				if (typeof(List<int>).IsAssignableFrom(method.ReturnType) && method.ReturnType != typeof(string)) {
					List<int> result = (List<int>)method.Invoke(o, null);
					Console.WriteLine($"\t{method.Name}=" + string.Join(", ", result));
				}
			}
			foreach (System.Reflection.FieldInfo fieldInfo in o.GetType().GetFields(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance)) {
				Console.WriteLine($"\t{fieldInfo.Name}={fieldInfo.GetValue(o)}");
			}
		}
	}
}
