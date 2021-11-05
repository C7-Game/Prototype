namespace C7GameData
{
    using System.Collections.Generic;
    public class GameData
    {
        public int turn {get; set;}
        public GameMap map {get; set;}
        List<TerrainType> terrainTypes = new List<TerrainType>();

        public List<MapUnit> mapUnits {get;} = new List<MapUnit>();
        List<UnitPrototype> unitPrototypes = new List<UnitPrototype>();

        /**
         * This is a placeholder method that creates a super skeletal set of game data,
         * so we can build out the most basic mechanics.
         *
         * I expect that Puppeteer will tack on some randomly generated map data to
         * what this generates, but as we build out more infrastructure, we'll migrate
         * to a more proper game state generation technique, be that reading from a BIQ
         * initially, or something else.
         **/
        public void createDummyGameData()
        {
            this.turn = 0;
            this.map = GameMap.generateDummyGameMap();

            //Right now, the one terrain type is in the map but not in our list here.
            //That is not great, but let's overlook that for now, as for now all our terrain type
            //references will be via the map.

            UnitPrototype settler = new UnitPrototype();
            settler.name = "Settler";
            settler.attack = 0;
            settler.defense = 0;
            settler.movement = 0;
            settler.canFoundCity = true;

            UnitPrototype warrior = new UnitPrototype();
            warrior.name = "Warrior";
            warrior.attack = 1;
            warrior.defense = 1;
            warrior.movement = 1;

            UnitPrototype worker = new UnitPrototype();
            worker.name = "Worker";
            worker.attack = 0;
            worker.defense = 0;
            worker.movement = 1;
            worker.canBuildRoads = true;

            MapUnit mapSettler = new MapUnit();
            mapSettler.unitType = settler;
            //right, a convenience method for setting the tile based on X, Y would be handy
            //we'll just hard-code a tile for now.
            mapSettler.location = map.tiles[168];
            mapUnits.Add(mapSettler);

            MapUnit mapWarrior = new MapUnit();
            mapWarrior.unitType = warrior;
            mapWarrior.location = map.tiles[168];
            mapUnits.Add(mapWarrior);

            MapUnit mapWorker = new MapUnit();
            mapWorker.unitType = worker;
            mapWorker.location = map.tiles[168];
            mapUnits.Add(mapWorker);

            //Cool, an entire game world has been created.  Now the user can do things with this super exciting hard-coded world!
        }
    }
}