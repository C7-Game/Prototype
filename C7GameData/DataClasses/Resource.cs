using System;
using System.Collections.Generic;

namespace C7GameData.DataClasses
{
    // A resource type (eg. Rubber, Gold)
    public class Resource
    {
        public Tech requiredTech;
        public List<Building> buildingsThatRequire;
        public List<Action> actionsThatRequire;
        public List<Unit> unitsThatRequire;
        public List<Tile> resourceLocations;
        public List<Terrain> foundOn;
    }
}
