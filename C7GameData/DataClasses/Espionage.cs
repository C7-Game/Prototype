using System;
using System.Collections.Generic;

namespace C7GameData.DataClasses
{
    // A type of diplomatic or spy mission
    public class Espionage
    {
        public Building requiredBuilding;
        public List<CivCivEspionage> missions;
    }
}
