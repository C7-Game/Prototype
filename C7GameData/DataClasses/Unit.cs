using System;
using System.Collections.Generic;

namespace C7GameData.DataClasses
{
    // A unit type (eg. Warrior, Worker)
    public class Unit
    {
        public List<Resource> requiredResources;
        public Tech requiredTech;
        public List<Unit> upgradesTo;
        public List<Unit> upgradesFrom;
        public List<Building> buildingsThatProduce;
        public List<Action> actions;
        public List<Race> allowedRaces;
        public List<TileUnit> units; // List of units on the map, as opposed to the prototype
        public List<Terrain> notAllowedOn;
        public List<Terrain> costFreeMovementOn;
    }
}
