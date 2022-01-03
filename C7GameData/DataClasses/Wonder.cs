using System;
using System.Collections.Generic;

namespace C7GameData.DataClasses
{
    // A small or great wonder
    public class Wonder: Building
    {
        public Tech obsoleteAtTech;
        public List<Espionage> espionagesUnlocked;
        public Building buildingAffected;
    }
}
