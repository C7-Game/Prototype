namespace C7GameData
{
    using System;
    using System.Collections.Generic;
    public class GameData
    {
        public int turn {get; set;}
        private Random rng; // TODO: Is GameData really the place for this?
        public GameMap map {get; set;}
        public List<Player> players = new List<Player>();
        public List<TerrainType> terrainTypes = new List<TerrainType>();

        public List<MapUnit> mapUnits {get;} = new List<MapUnit>();
        public Dictionary<string, UnitPrototype> unitPrototypes = new Dictionary<string, UnitPrototype>();
        public List<City> cities = new List<City>();

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
            this.rng = new Random();
            
            CreateDefaultUnitPrototypes();
            
            int white = -1; // = 0xFFFFFFFF, but we can't just use that b/c the compiler complains about uint-to-int conversion
            Player barbarianPlayer = new Player(white);
            barbarianPlayer.isBarbarians = true;
            players.Add(barbarianPlayer);

            int blue = 0x4040FFFF; // R:64, G:64, B:255, A:255
            Player humanPlayer = new Player(blue);
            players.Add(humanPlayer);

            int green = 0x00FF00FF;
            Player computerPlayOne = new Player(green);
            players.Add(computerPlayOne);

            int teal = 0x40FFFFFF;
            Player computerPlayerTwo = new Player(teal);
            players.Add(computerPlayerTwo);

            int purple = 0x6040D0FF;
            Player computerPlayerThree = new Player(purple);
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
            UnitPrototype warrior = new UnitPrototype();
            warrior.name = "Warrior";
            warrior.attack = 1;
            warrior.defense = 1;
            warrior.movement = 1;
            warrior.iconIndex = 6;
            warrior.shieldCost = 10;

            UnitPrototype settler = new UnitPrototype();
            settler.name = "Settler";
            settler.attack = 0;
            settler.defense = 0;
            settler.movement = 1;
            settler.iconIndex = 0;
            settler.canFoundCity = true;
            settler.shieldCost = 30;
            settler.populationCost = 2;

            UnitPrototype worker = new UnitPrototype();
            worker.name = "Worker";
            worker.attack = 0;
            worker.defense = 0;
            worker.movement = 1;
            worker.iconIndex = 1;
            worker.canBuildRoads = true;
            worker.shieldCost = 1;
            worker.populationCost = 1;

            UnitPrototype chariot = new UnitPrototype();
            chariot.name = "Chariot";
            chariot.attack = 1;
            chariot.defense = 1;
            chariot.movement = 2;
            chariot.iconIndex = 10;
            chariot.shieldCost = 30;

            UnitPrototype galley = new SeaUnit();
            galley.name = "Galley";
            galley.attack = 1;
            galley.defense = 1;
            galley.movement = 3;
            galley.iconIndex = 29;
            galley.shieldCost = 30;
            
            unitPrototypes["Warrior"] = warrior;
            unitPrototypes["Settler"] = settler;
            unitPrototypes["Worker"] = worker;
            unitPrototypes["Chariot"] = chariot;
            unitPrototypes["Galley"] = galley;
        }
    }
}
