using System;
using System.Collections.Generic;

namespace C7GameData.DataClasses
{
    // An instance of an espionage mission conducted by one civ against another
    public class CivCivEspionage
    {
        public CivCiv civCiv;
        public Espionage mission;
        public City targetCity;
    }
}
