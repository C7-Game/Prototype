using Serilog;

namespace C7GameData
{
	using System;
	using System.Collections.Generic;
	using System.Text.Json.Serialization;
	public class GameData {
		[JsonIgnore]
		private static ILogger log = Log.ForContext<GameData>();

		public int seed = -1;	//change here to set a hard-coded seed
		public int turn {get; set;}
		public static Random rng; // TODO: Is GameData really the place for this?
		public IDFactory ids {get; private set;} = new IDFactory();
		public GameMap map {get; set;}
		public List<Player> players = new List<Player>();
		public List<TerrainType> terrainTypes = new List<TerrainType>();
		public List<Resource> Resources = new List<Resource>();
		public List<MapUnit> mapUnits {get;} = new List<MapUnit>();
		public Dictionary<string, UnitPrototype> unitPrototypes = new Dictionary<string, UnitPrototype>();
		public List<City> cities = new List<City>();

		public List<ExperienceLevel> experienceLevels = new List<ExperienceLevel>();
		public string defaultExperienceLevelKey;
		[JsonIgnore]
		public ExperienceLevel defaultExperienceLevel;

		public BarbarianInfo barbarianInfo = new BarbarianInfo();

		public StrengthBonus fortificationBonus;
		public StrengthBonus riverCrossingBonus;
		public StrengthBonus cityLevel1DefenseBonus;
		public StrengthBonus cityLevel2DefenseBonus;
		public StrengthBonus cityLevel3DefenseBonus;

		public int healRateInFriendlyField;
		public int healRateInNeutralField;
		public int healRateInHostileField;
		public int healRateInCity;

		public bool observerMode = false;

		public string scenarioSearchPath;	//legacy from Civ3, we'll probably have a more modern format someday but this keeps legacy compatibility

		public GameData()
		{
			map = new GameMap();
			if (seed == -1) {
				rng = new Random();
				seed = rng.Next(int.MaxValue);
				log.Information("Random seed is " + seed);
			}
			rng = new Random(seed);
		}

		public List<Player> GetHumanPlayers() {
			return players.FindAll(p => p.isHuman);
		}

		public MapUnit GetUnit(ID id)
		{
			return mapUnits.Find(u => u.id == id);
		}

		public ExperienceLevel GetExperienceLevelAfter(ExperienceLevel experienceLevel)
		{
			int n = experienceLevels.IndexOf(experienceLevel);
			if (n + 1 < experienceLevels.Count)
				return experienceLevels[n + 1];
			else
				return null;
		}

		/**
		 * This is intended as a place to set up post-load actions on the save, regardless of
		 * whether it is loaded from a legacy Civ3 file or a C7 native file.
		 * This likely is any sort of calculation which is useful to have in the game state, but
		 * can be re-generated from save data and does not make sense to serialize.
		 **/
		public void PerformPostLoadActions()
		{
			//Let each tile know who its neighbors are.  It needs to know this so its graphics can be selected appropriately.
			map.computeNeighbors();
		}

		/**
		 * This is a placeholder method that creates a super skeletal set of game data,
		 * so we can build out the most basic mechanics.
		 *
		 * I expect that Puppeteer will tack on some randomly generated map data to
		 * what this generates, but as we build out more infrastructure, we'll migrate
		 * to a more proper game state generation technique, be that reading from a BIQ
		 * initially, or something else.
		 *
		 * Returns the human player so the caller (which is the UI) can store it.
		 **/
		public Player CreateDummyGameData()
		{
			this.turn = 0;

			uint white = 0xFFFFFFFF;
			Player barbarianPlayer = new Player(white);
			barbarianPlayer.isBarbarians = true;
			players.Add(barbarianPlayer);

			Civilization carthage = new Civilization();
			carthage.cityNames.Add("Carthage");
			carthage.cityNames.Add("Utica");
			carthage.cityNames.Add("Hadrumetum");
			carthage.cityNames.Add("Hippo");
			carthage.cityNames.Add("Kerkouane");
			carthage.cityNames.Add("Lilybaeum");
			carthage.cityNames.Add("Motya");
			carthage.cityNames.Add("Thignica");
			carthage.cityNames.Add("Zama");
			carthage.cityNames.Add("Thabraca");
			carthage.cityNames.Add("Panormus");
			uint grey = 0xA0A0A0FF;
			Player carthagePlayer = new Player(carthage, grey);
			carthagePlayer.isHuman = true;
			players.Add(carthagePlayer);

			Civilization rome = new Civilization();
			rome.cityNames.Add("Rome");
			rome.cityNames.Add("Neapolis");
			rome.cityNames.Add("Capua");
			rome.cityNames.Add("Tarentum");
			rome.cityNames.Add("Croton");
			rome.cityNames.Add("Placentia on the Trebia");
			rome.cityNames.Add("Cortona/Lake Trasimene");
			rome.cityNames.Add("Cannae");
			rome.cityNames.Add("Rhegium");
			rome.cityNames.Add("Pompeii");
			rome.cityNames.Add("Arettium");
			uint romanRed = 0xE00000FF;
			Player romePlayer = new Player(rome, romanRed);
			players.Add(romePlayer);

			Civilization babylon = new Civilization();
			babylon.cityNames.Add("Babylon");
			babylon.cityNames.Add("Kish");
			babylon.cityNames.Add("Sippar");
			babylon.cityNames.Add("Nippur");
			babylon.cityNames.Add("Isin");
			babylon.cityNames.Add("Eshnunna");
			babylon.cityNames.Add("Uruk");
			babylon.cityNames.Add("Larsa");
			babylon.cityNames.Add("Lagash");
			babylon.cityNames.Add("Ur");
			babylon.cityNames.Add("Eridu");
			babylon.cityNames.Add("Malgium");
			babylon.cityNames.Add("Rapiqum");
			babylon.cityNames.Add("Mari");
			babylon.cityNames.Add("Nineveh");

			uint blue = 0x4040FFFF; // R:64, G:64, B:255, A:255
			Player babylonPlayer = new Player(babylon, blue);
			babylonPlayer.isHuman = false;
			players.Add(babylonPlayer);

			Civilization greece = new Civilization();
			greece.cityNames.Add("Athens");
			greece.cityNames.Add("Sparta");
			greece.cityNames.Add("Corinth");
			greece.cityNames.Add("Delphi");
			greece.cityNames.Add("Thebes");
			greece.cityNames.Add("Argos");
			greece.cityNames.Add("Larissa");
			greece.cityNames.Add("Olympia");
			greece.cityNames.Add("Tegea");
			greece.cityNames.Add("Thessaloniki");
			greece.cityNames.Add("Miletus");

			uint green = 0x00EE00FF;
			Player computerPlayOne = new Player(greece, green);
			players.Add(computerPlayOne);

			Civilization america = new Civilization();
			america.cityNames.Add("Philadelphia");
			america.cityNames.Add("New York");
			america.cityNames.Add("Boston");
			america.cityNames.Add("Charleston");
			america.cityNames.Add("Baltimore");
			america.cityNames.Add("Washington");
			america.cityNames.Add("Providence");
			america.cityNames.Add("New Haven");
			america.cityNames.Add("Norfolk");
			america.cityNames.Add("New Orleans");
			america.cityNames.Add("Cincinnati");
			america.cityNames.Add("St. Louis");
			america.cityNames.Add("Albany");
			america.cityNames.Add("Pittsburgh");
			america.cityNames.Add("Louisville");

			uint teal = 0x40EEEEFF;
			Player computerPlayerTwo = new Player(america, teal);
			players.Add(computerPlayerTwo);

			Civilization theNetherlands = new Civilization();
			theNetherlands.cityNames.Add("Amsterdam");
			theNetherlands.cityNames.Add("Rotterdam");
			theNetherlands.cityNames.Add("The Hague");
			theNetherlands.cityNames.Add("Utrecht");
			theNetherlands.cityNames.Add("Eindhoven");
			theNetherlands.cityNames.Add("Groningen");
			theNetherlands.cityNames.Add("Arnhem");
			theNetherlands.cityNames.Add("Tilburg");
			theNetherlands.cityNames.Add("Maastricht");

			uint orange = 0xFFAB12FF;
			Player computerPlayerThree = new Player(theNetherlands, orange);
			players.Add(computerPlayerThree);

			List<Tile> startingLocations = map.generateStartingLocations(players.Count, 10);

			int i = 0;
			foreach (Player player in players)
			{
				if (player.isBarbarians) {
					continue;
				}
				CreateStartingDummyUnits(player, startingLocations[i]);
				i++;
			}

			//Todo: We're using the same method for start locs and barb camps.  Really, we should use a similar one for
			//variation, but the barb camp one should also take into account things like not spawning in revealed
			//tiles.  It also would be really nice to be able to generate them separately and not have them overlap.
			List<Tile> barbarianCamps = map.generateStartingLocations(10, 10);
			foreach (Tile barbCampLocation in barbarianCamps) {
				if (barbCampLocation.unitsOnTile.Count == 0) { // in case a starting location is under one of the human player's units
					MapUnit barbarian = CreateDummyUnit(barbarianInfo.basicBarbarian, barbarianPlayer, barbCampLocation);
					barbarian.isFortified = true; // Can't do this through UnitInteractions b/c we don't have access to the engine. Really this
					// whole procedure of generating a map should be part of the engine not the data module.
					barbarian.facingDirection = TileDirection.SOUTHEAST;
					barbarian.location.hasBarbarianCamp = true;
					map.barbarianCamps.Add(barbCampLocation);
				}
			}


			//Cool, an entire game world has been created.  Now the user can do things with this super exciting hard-coded world!

			return carthagePlayer;
		}

		private void CreateStartingDummyUnits(Player player, Tile location) {
			string[] unitNames = { "Settler", "Warrior", "Worker", "Chariot", "Catapult" };
			foreach (string unitName in unitNames)
			{
				if (unitPrototypes.ContainsKey(unitName)) {
					CreateDummyUnit(unitPrototypes[unitName],  player, location);
				}
			}
		}

		private MapUnit CreateDummyUnit(UnitPrototype proto, Player owner, Tile tile)
		{
			//TODO: The fact that we have to check for this makes me wonder why...
			if (tile != Tile.NONE) {
				//TODO: Generate unit from its prototype
				MapUnit unit = new MapUnit(this.ids.CreateID(proto.name));
				unit.unitType = proto;
				unit.owner = owner;
				unit.location = tile;
				unit.experienceLevelKey = defaultExperienceLevelKey;
				unit.experienceLevel = defaultExperienceLevel;
				unit.facingDirection = TileDirection.SOUTHWEST;
				unit.movementPoints.reset(proto.movement);
				unit.hitPointsRemaining = 3;
				tile.unitsOnTile.Add(unit);
				//TODO: Probably remove mapUnits
				mapUnits.Add(unit);
				owner.AddUnit(unit);
				owner.tileKnowledge.AddTilesToKnown(tile);
				return unit;
			} else
				throw new System.Exception("Tried to add dummy unit at Tile.NONE");
		}
	}
}
