using System;
using System.Collections.Generic;

namespace C7GameData.DataClasses
{
    // A worker action, though not necessarily constrained to just workers (eg. build fortress, chop forest)
    public class Action
    {
        public List<Unit> unitsThatCanPerform;
        public Tech requiredTech;
        public Overlay affectedOverlay;
    }
}
