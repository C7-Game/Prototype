using System;
using System.Collections.Generic;

namespace C7GameData.DataClasses
{
    // Civ-to-civ relationship
    public class CivCiv
    {
        public Civ fromCiv;
        public Civ toCiv;
        public List<Trade> trades;
        public List<CivCivEspionage> missions;
    }
}
