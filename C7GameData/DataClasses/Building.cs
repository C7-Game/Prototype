using System;
using System.Collections.Generic;

namespace C7GameData.DataClasses
{
    // A building type (eg. Marketplace, Factory)
    public class Building: Producible
    {
        public Unit producesUnit;
        public Government requiredGovernment;
        public Building requiredBuilding;
        public List<Building> buildingsUnlocked;
        public List<Trait> traits;
        public List<CityBuilding> buildings;
        public Building affectedBy;
    }
}
