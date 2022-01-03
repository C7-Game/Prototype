using System;
using System.Collections.Generic;

namespace C7GameData.DataClasses
{
    // A tech
    public class Tech
    {
        public List<Unit> unitsUnlocked;
        public List<Resource> resourcesUnlocked;
        public List<Action> actionsUnlocked;
        public List<Building> buildingsUnlocked;
        public List<Citizen> citizensUnlocked;
        public List<Government> governmentsUnlocked;
        public List<Tech> requiredTechs;
        public List<Tech> prereqFor;
        public List<Civ> researchedBy;
        public List<Civ> beingResearchedBy;
        public List<Trait> freeTechFor;
        public Era era;
        public List<Building> buildingsMadeObsolete;
        public Tech backupFreeTech;
        public Tech backupOf;
    }
}
