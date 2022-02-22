using System.Drawing;

namespace C7GameData
{
    using System;
    using System.Collections.Generic;
    public class GameData
    {
        public int turn {get; set;}
        public Random rng; // TODO: Is GameData really the place for this?
        public GameMap map {get; set;}
        public List<Player> players = new List<Player>();
        public List<TerrainType> terrainTypes = new List<TerrainType>();

        public List<MapUnit> mapUnits {get;} = new List<MapUnit>();
        public Dictionary<string, UnitPrototype> unitPrototypes = new Dictionary<string, UnitPrototype>();
        public List<City> cities = new List<City>();

        public GameData()
        {
	        map = new GameMap();
	        rng = new Random();
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
            foreach (Tile tile in map.tiles) {
                Dictionary<TileDirection, Tile> neighbors = new Dictionary<TileDirection, Tile>();
                foreach (TileDirection direction in Enum.GetValues(typeof(TileDirection))) {
                    neighbors[direction] = map.tileNeighbor(tile, direction);
                }
                tile.neighbors = neighbors;
            }
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
            
            CreateDefaultUnitPrototypes();
            
            uint white = 0xFFFFFFFF;
            Player barbarianPlayer = new Player(white);
            barbarianPlayer.isBarbarians = true;
            players.Add(barbarianPlayer);

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
            Player humanPlayer = new Player(babylon, blue);
            humanPlayer.isHuman = true;
            players.Add(humanPlayer);

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

			List<Tile> startingLocations = map.generateStartingLocations(rng, 4, 10);

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
            List<Tile> barbarianCamps = map.generateStartingLocations(rng, 10, 10);
            foreach (Tile barbCampLocation in barbarianCamps) {
                if (barbCampLocation.unitsOnTile.Count == 0) { // in case a starting location is under one of the human player's units
                    MapUnit barbWarrior = CreateDummyUnit(unitPrototypes["Warrior"], barbarianPlayer, barbCampLocation);
                    barbWarrior.isFortified = true; // Can't do this through UnitInteractions b/c we don't have access to the engine. Really this
                    // whole procedure of generating a map should be part of the engine not the data module.
                    barbWarrior.facingDirection = TileDirection.SOUTHEAST;
                    barbWarrior.location.hasBarbarianCamp = true;
                    map.barbarianCamps.Add(barbCampLocation);
                }
            }


            //Cool, an entire game world has been created.  Now the user can do things with this super exciting hard-coded world!

            return humanPlayer;
        }
        
		private void CreateStartingDummyUnits(Player player, Tile location)
		{
			CreateDummyUnit(unitPrototypes["Settler"], player, location);
			CreateDummyUnit(unitPrototypes["Warrior"], player, location);
			CreateDummyUnit(unitPrototypes["Worker"],  player, location);
			CreateDummyUnit(unitPrototypes["Chariot"], player, location);
		}

		private MapUnit CreateDummyUnit(UnitPrototype proto, Player owner, Tile tile)
		{
			//TODO: The fact that we have to check for this makes me wonder why...
			if (tile != Tile.NONE) {
				//TODO: Generate unit from its prototype
				MapUnit unit = new MapUnit();
				unit.unitType = proto;
				unit.owner = owner;
				unit.location = tile;
				unit.facingDirection = TileDirection.SOUTHWEST;
				unit.movementPointsRemaining = proto.movement;
				unit.hitPointsRemaining = 3;
				tile.unitsOnTile.Add(unit);
				//TODO: Probably remove mapUnits
				mapUnits.Add(unit);
				owner.AddUnit(unit);
				return unit;
			} else
				throw new System.Exception("Tried to add dummy unit at Tile.NONE");
		}
        private void CreateDefaultUnitPrototypes()
        {
			unitPrototypes = new Dictionary<string, UnitPrototype>()
			{
				{ "Warrior", new UnitPrototype { name = "Warrior", attack = 1, defense = 1, movement = 1, iconIndex =  6, shieldCost = 10 }},
				{ "Settler", new UnitPrototype { name = "Settler", attack = 0, defense = 0, movement = 1, iconIndex =  0, shieldCost = 30, populationCost = 2 }},
				{ "Worker",  new UnitPrototype { name = "Worker",  attack = 0, defense = 0, movement = 1, iconIndex =  1, shieldCost = 30, populationCost = 1 }},
				{ "Chariot", new UnitPrototype { name = "Chariot", attack = 1, defense = 1, movement = 2, iconIndex = 10, shieldCost = 20 }},
				{ "Galley",  new SeaUnit       { name = "Galley",  attack = 1, defense = 1, movement = 3, iconIndex = 29, shieldCost = 30 }},
			};
        }
    }
}
