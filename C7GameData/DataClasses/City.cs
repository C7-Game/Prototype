using System;
using System.Collections.Generic;

namespace C7GameData.DataClasses
{
    public enum ProductionType {
        Unit,
        Building,
        Wealth,
    }

    // A city on the map
    public class City
    {
        public Tile tile;
        public List<CityCitizen> citizens;
        public Civ civ;
        public List<CityBuilding> buildings;
        public List<CivCivEspionage> targetOfMissions;
        // productionQueue and productionTypeQueue should always be the same length
        // a city can produce buildings, units, or wealth
        // however, only Building and Unit are subtypes of Producible, so for wealth, the item in productionQueue should be set to null
        public Queue<Producible> productionQueue;
        public Queue<ProductionType> productionTypeQueue;

        public string name;
        public int population;
        public int food;
        public int culture;
        public int shields;
        public int turnFounded;
        public int draftedThisTurn;
    }
}
