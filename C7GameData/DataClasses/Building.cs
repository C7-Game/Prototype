using System;
using System.Collections.Generic;

namespace C7GameData.DataClasses
{
    // A building type (eg. Marketplace, Factory)
    public class Building
    {
        public Unit producesUnit;
        public List<Resource> requiredResources;
        public Tech requiredTech;
        public Government requiredGovernment;
        public Building requiredBuilding;
        public List<Building> buildingsUnlocked;
        public List<Trait> traits;
        public List<City> citiesWith;
        public Building affectedBy;
    }
}
