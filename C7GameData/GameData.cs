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
        List<UnitPrototype> unitPrototypes = new List<UnitPrototype>();
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

        public MapUnit createDummyUnit(UnitPrototype proto, Player owner, int tileX, int tileY)
        {
            if (map.isTileAt(tileX, tileY)) {
                var tile = map.tileAt(tileX, tileY);
                var unit = new MapUnit();
                unit.unitType = proto;
		unit.owner = owner;
                unit.location = tile;
                tile.unitsOnTile.Add(unit);
                mapUnits.Add(unit);
                unit.movementPointsRemaining = proto.movement;
                unit.hitPointsRemaining = 3;
                return unit;
            } else
                throw new System.Exception("Invalid tile coordinates");
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
        public Player createDummyGameData()
        {
            this.turn = 0;
            this.rng = new Random();

            int blue = 0x4040FFFF; // R:64, G:64, B:255, A:255
            var humanPlayer = new Player(blue);
            players.Add(humanPlayer);

            int white = -1; // = 0xFFFFFFFF, but we can't just use that b/c the compiler complains about uint-to-int conversion
            var barbarianPlayer = new Player(white);
            players.Add(barbarianPlayer);

            //Right now, the one terrain type is in the map but not in our list here.
            //That is not great, but let's overlook that for now, as for now all our terrain type
            //references will be via the map.

            UnitPrototype settler = new UnitPrototype();
            settler.name = "Settler";
            settler.attack = 0;
            settler.defense = 0;
            settler.movement = 1;
            settler.iconIndex = 0;
            settler.canFoundCity = true;

            UnitPrototype warrior = new UnitPrototype();
            warrior.name = "Warrior";
            warrior.attack = 1;
            warrior.defense = 1;
            warrior.movement = 1;
            warrior.iconIndex = 6;

            UnitPrototype worker = new UnitPrototype();
            worker.name = "Worker";
            worker.attack = 0;
            worker.defense = 0;
            worker.movement = 1;
            worker.iconIndex = 1;
            worker.canBuildRoads = true;

            UnitPrototype chariot = new UnitPrototype();
            chariot.name = "Chariot";
            chariot.attack = 1;
            chariot.defense = 1;
            chariot.movement = 2;
            chariot.iconIndex = 10;

            createDummyUnit(settler, humanPlayer,  20, 26);
            createDummyUnit(warrior, humanPlayer,  22, 26);
            createDummyUnit(worker , humanPlayer, 24, 26);
            createDummyUnit(chariot, humanPlayer, 26, 26);

            var startingLocations = map.generateStartingLocations(rng, 10, 10);
            foreach (var sL in startingLocations)
                if (sL.unitsOnTile.Count == 0) { // in case a starting location is under one of the human player's units
                    var barbWarrior = createDummyUnit(warrior, barbarianPlayer, sL.xCoordinate, sL.yCoordinate);
                    barbWarrior.isFortified = true; // Can't do this through UnitInteractions b/c we don't have access to the engine. Really this
                                                    // whole procedure of generating a map should be part of the engine not the data module.
                    barbWarrior.location.hasBarbarianCamp = true;
                }

            //Cool, an entire game world has been created.  Now the user can do things with this super exciting hard-coded world!

	    return humanPlayer;
        }
    }
}
