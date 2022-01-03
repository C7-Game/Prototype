using System;
using System.Collections.Generic;

namespace C7GameData.DataClasses
{
    // A type of citizen/specialist (eg. Entertainer, Tax Collector)
    public class Citizen
    {
        public List<CityCitizen> citizens; // instances of citizens in cities of this type
        public Tech requiredTech;
    }
}
