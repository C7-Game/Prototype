namespace C7Engine
{
    using C7GameData;
    using System;
    using System.Collections.Generic;

    public class UnitInteractions
    {

        private static Queue<MapUnit> waitQueue = new Queue<MapUnit>();

        /**
         * Returns all units.  Should be used sparingly.
         **/
        public static List<MapUnit> GetAllUnits()
        {
            return EngineStorage.gameData.mapUnits;
        }

        public static MapUnit getNextSelectedUnit()
        {
            GameData gameData = EngineStorage.gameData;
            foreach (MapUnit unit in gameData.mapUnits)
            {
                //Eventually we'll have to check ownership,
                //but we haven't added the concepts of players or civilizations yet.
                if (unit.movementPointsRemaining > 0 && !unit.isFortified)
                {
                    if (!waitQueue.Contains(unit)) {
                        return UnitWithAvailableActions(unit);
                    }
                }
            }
            if (waitQueue.Count > 0) {
                return waitQueue.Dequeue();
            }
            return MapUnit.NONE;
        }

        /**
         * Helper function to add the available actions to a unit
         * based on what terrain it is on.
         **/
        private static MapUnit UnitWithAvailableActions(MapUnit unit)
        {
            unit.availableActions.Clear();

            //This should have "real" code someday.  For now, I'll hard-code a few things based
            //on the unit type.  That will allow proving the end-to-end works, and we can
            //add real support as we add more mechanics.  Probably some of it early, some of it...
            //not so early.
            //For now, we'll add 'all' the basic actions (e.g. vanilla, non-automated ones), though this is not necessarily right.
            string[] basicActions = { "hold", "wait", "fortify", "disband", "goTo"};
            unit.availableActions.AddRange(basicActions);

            string unitType = unit.unitType.name;
            if (unitType.Equals("Warrior")) {
                unit.availableActions.Add("pillage");
            }
            else if (unitType.Equals("Settler")) {
                unit.availableActions.Add("buildCity");
            }
            else if (unitType.Equals("Worker")) {
                unit.availableActions.Add("road");
                unit.availableActions.Add("mine");
                unit.availableActions.Add("irrigate");
            }
            else if (unit.unitType.Equals("Chariot")) {
                unit.availableActions.Add("pillage");
            }
            else {
                //It must be a catapult
                unit.availableActions.Add("bombard");
            }

            //Always add an advanced action b/c we don't have code to make the buttons show up at the right spot if they're all hidden yet
            unit.availableActions.Add("rename");

            return unit;
        }

        public static void fortifyUnit(string guid)
        {
            new MsgFortifyUnit(guid).send();
        }

        public static void moveUnit(string guid, TileDirection dir)
        {
            new MsgMoveUnit(guid, dir).send();
        }

        public static void holdUnit(string guid)
        {
            new MsgSkipUnitTurn(guid).send();
        }

        public static void waitUnit(string guid)
        {
            GameData gameData = EngineStorage.gameData;
            foreach (MapUnit unit in gameData.mapUnits)
            {
                if (unit.guid == guid)
                {
                    Console.WriteLine("Found matching unit with guid " + guid + " of type " + unit.GetType().Name + "; adding it to the wait queue");
                    waitQueue.Enqueue(unit);
                }
            }
            Console.WriteLine("Failed to find a matching unit with guid " + guid);
        }

        public static void disbandUnit(string guid)
        {
            GameData gameData = EngineStorage.gameData;
            MapUnit toBeDeleted = null;
            foreach (MapUnit unit in gameData.mapUnits)
            {
                if (unit.guid == guid)
                {
                    //Set a variable and break so we don't cause a ConcurrentModificationException
                    toBeDeleted = unit;
                    break;
                }
            }
            if (toBeDeleted != null) {
                EngineStorage.animTracker.endAnimation(toBeDeleted, false);
                toBeDeleted.location.unitsOnTile.Remove(toBeDeleted);
                gameData.mapUnits.Remove(toBeDeleted);
            }
        }

        public static void ClearWaitQueue()
        {
            waitQueue.Clear();
        }
    }
}

