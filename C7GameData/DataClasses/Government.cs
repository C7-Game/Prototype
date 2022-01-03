using System;
using System.Collections.Generic;

namespace C7GameData.DataClasses
{
    // A government type (eg. Anarchy, Democracy)
    public class Government
    {
        public Tech requiredTech;
        public List<Building> allowedBuildings;
        public List<Race> racesWherePreferred;
        public List<Race> racesWhereScorned;
        public List<Civ> civsUsing;
        public List<GovernmentGovernment> relationships;
    }
}
