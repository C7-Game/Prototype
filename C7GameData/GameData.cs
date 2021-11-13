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

        public MapUnit createDummyUnit(UnitPrototype proto, int tileX, int tileY)
        {
            if (map.isTileAt(tileX, tileY)) {
                var tile = map.tileAt(tileX, tileY);
                var unit = new MapUnit();
                unit.unitType = proto;
                unit.location = tile;
                tile.unitsOnTile.Add(unit);
                mapUnits.Add(unit);
                unit.movementPointsRemaining = proto.movement;
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

            createDummyUnit(settler,  6, 6);
            createDummyUnit(warrior,  8, 6);
            createDummyUnit(worker , 10, 6);

            //Cool, an entire game world has been created.  Now the user can do things with this super exciting hard-coded world!
        }
    }
}
