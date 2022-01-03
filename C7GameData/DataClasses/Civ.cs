using System;
using System.Collections.Generic;

namespace C7GameData.DataClasses
{
    // A civilization
    public class Civ
    {
        public List<TileUnit> units;
        public List<City> cities;
        public List<CivTile> tiles;
        public Race race;
        public Palace palace;
        public Government government;
        public List<Tech> techsResearched;
        public Tech researching;
        public List<CivCiv> relationships;
    }
}
