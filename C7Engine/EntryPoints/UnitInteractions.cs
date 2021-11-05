namespace C7Engine
{
    using C7GameData;
    public class UnitInteractions
    {
        public static MapUnit getNextSelectedUnit()
        {
            GameData gameData = EngineStorage.gameData;
            foreach (MapUnit unit in gameData.mapUnits)
            {
                //Eventually we'll have to check ownership,
                //but we haven't added the concepts of players or civilizations yet.
                if (unit.movementPointsRemaining > 0 && !unit.isFortified)
                {
                    return unit;
                }
            }
            //We probably shouldn't return null and tempt fate
            return null;
        }

        public static void fortifyUnit(string guid)
        {
            GameData gameData = EngineStorage.gameData;
            //This is inefficient, perhaps we'll have a map someday.  But with three units,
            //we'll survive for now.
            foreach (MapUnit unit in gameData.mapUnits)
            {
                if (unit.guid == guid)
                {
                    unit.isFortified = true;
                }
            }
        }

        /**
         * Super dumb movement where you can only move to one tile, and back to the first one.
         * We'll build out more movement later.
         **/
        public static void moveUnit(string guid)
        {
            GameData gameData = EngineStorage.gameData;
            //This is inefficient, perhaps we'll have a map someday.  But with three units,
            //we'll survive for now.
            foreach (MapUnit unit in gameData.mapUnits)
            {
                if (unit.guid == guid)
                {
                    if (unit.location == gameData.map.tiles[168])
                    {
                        unit.location = gameData.map.tiles[169];
                    }
                    else
                    {
                        unit.location = gameData.map.tiles[168];
                    }
                }
            }
        }
    }
}