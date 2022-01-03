using System;
using System.Collections.Generic;

namespace C7GameData.DataClasses
{
    // An instance of a building in a city
    public class CityBuilding
    {
        public City city;
        public Building buildingType;

        public int turnBuilt;
        public int cultureProduced;
    }
}
