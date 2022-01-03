using System;
using System.Collections.Generic;

namespace C7GameData.DataClasses
{
    // Any immobile thing that can appear on a tile that isn't a city, resource, or terrain
    // (eg. forest, marsh, road, mine, fortress, outpost, colony, radar tower, etc)
    public class Overlay: Terrain
    {
        public Action addingAction;
        public Action removingAction;
        public Overlay requiredOverlay;
        public List<Overlay> overlaysAllowed;
        public List<Overlay> compatibleOverlays;
    }
}
