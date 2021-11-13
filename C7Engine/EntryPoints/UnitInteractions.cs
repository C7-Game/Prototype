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
            GameData gameData = EngineStorage.gameData;
            //This is inefficient, perhaps we'll have a map someday.  But with three units,
            //we'll survive for now.
            Console.WriteLine("Trying to fortify unit " + guid);
            foreach (MapUnit unit in gameData.mapUnits)
            {
                if (unit.guid == guid)
                {
                    Console.WriteLine("Set unit " + guid + " of type " + unit.GetType().Name + " to fortified");
                    unit.isFortified = true;
                    return;
                }
            }
            Console.WriteLine("Failed to find unit " + guid);
        }

        // Moves a unit into a neighboring tile. direction = 1 for NE, 2 for E, 3 for SE, ..., 7 for NW, 8 for N. If direction is not in [1, 8],
        // this function does nothing.
        // TODO: This movement function is still pretty basic since it only works for neighboring tiles. I would call this function something like
        // stepUnit except it's the only unit movement function we have right now so calling it moveUnit is fine. But later we might want a more
        // powerful moveUnit function that can accept non-neighboring tiles and/or do things like rebase air units. Also direction should be an
        // enum or something.
        public static void moveUnit(string guid, int direction)
            {
                GameData gameData = EngineStorage.gameData;
                //This is inefficient, perhaps we'll have a map someday.  But with three units,
                //we'll survive for now.
                foreach (MapUnit unit in gameData.mapUnits)
                {
                    if (unit.guid == guid)
                    {
                        int dx, dy;
                        switch (direction) {
                        case 1: dx =  1; dy = -1; break;
                        case 2: dx =  2; dy =  0; break;
                        case 3: dx =  1; dy =  1; break;
                        case 4: dx =  0; dy =  2; break;
                        case 5: dx = -1; dy =  1; break;
                        case 6: dx = -2; dy =  0; break;
                        case 7: dx = -1; dy = -1; break;
                        case 8: dx =  0; dy = -2; break;
                        default:
                            return;
                        }

                        var newLoc = gameData.map.tileAt(dx + unit.location.xCoordinate, dy + unit.location.yCoordinate);
                        if ((newLoc != null) && (unit.movementPointsRemaining > 0)) {
                            if (! unit.location.unitsOnTile.Remove(unit))
                                throw new System.Exception("Failed to remove unit from tile it's supposed to be on");
                            newLoc.unitsOnTile.Add(unit);
                            unit.location = newLoc;
                            unit.movementPointsRemaining -= 1;
                        }

                        break;
                    }
                }
            }

        /**
         * I'd like to enhance this so it's like Civ4, where the hold action takes
         * the unit out of the rotation, but you can change your mind if need be.
         * But for now it'll be like Civ3, where you're out of luck if you realize
         * that unit was needed for something; that also simplifies things here.
         **/
        public static void holdUnit(string guid)
        {
            GameData gameData = EngineStorage.gameData;
            //This is inefficient, perhaps we'll have a map someday.  But with three units,
            //we'll survive for now.
            foreach (MapUnit unit in gameData.mapUnits)
            {
                if (unit.guid == guid)
                {
                    Console.WriteLine("Found matching unit with guid " + guid + " of type " + unit.GetType().Name + "; settings its movement to zero");
                    unit.movementPointsRemaining = 0;
                    return;
                }
            }
            Console.WriteLine("Failed to find a matching unit with guid " + guid);
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
