using System;
using System.Collections.Generic;

namespace C7GameData.DataClasses
{
    // An experience type (conscript, regular, etc)
    public class Experience
    {
        public List<TileUnit> units;

        public string name;
        public int baseHitPoints;
        public int retreatBonus;
    }
}
