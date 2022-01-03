using System;
using System.Collections.Generic;

namespace C7GameData.DataClasses
{
    // A race trait (eg. Militaristic, Expansionist)
    public class Trait
    {
        public List<Building> buildings;
        public Tech freeTech;
        public List<Race> racesWith;
    }
}
