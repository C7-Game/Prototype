using System;
using System.Collections.Generic;

namespace C7GameData.DataClasses
{
    // A terrain type (eg. plains, coast, hills)
    // Note that for our purposes here, forests and jungles and marshes are overlays, not terrains
    public class Terrain
    {
        public List<Tile> tiles;
        public List<Resource> resourcesAllowed;
        public List<TerrainOverlay> terrainOverlays;
        public List<Unit> restrictedUnits;
        public List<Unit> costFreeMovementUnits;
    }
}
