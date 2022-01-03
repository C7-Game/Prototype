using System;
using System.Collections.Generic;

namespace C7GameData.DataClasses
{
    // A playable race
    public class Race
    {
        public List<Trait> traits;
        public List<CityCitizen> cityCitizens;
        public List<TileUnit> units;
        public List<Unit> allowedUnits;
        public Government favoredGovernment;
        public Government scornedGovernment;
        public Civ civ; // Could make this a list if we want to allow multiple civs of the same race at once!
    }
}
