using System;
using System.Collections.Generic;

namespace C7GameData.DataClasses
{
    // A city on the map
    public class City
    {
        public Tile tile;
        public List<CityCitizen> citizens;
        public Civ civ;
        public List<Building> buildings;
        public List<CivCivEspionage> targetOfMissions;
    }
}
