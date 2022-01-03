using System;
using System.Collections.Generic;

namespace C7GameData.DataClasses
{
    // Something that can be created by a city (unit or building)
    public class Producible
    {
        public List<Resource> requiredResources;
        public Tech requiredTech;
    }
}
