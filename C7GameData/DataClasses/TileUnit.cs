using System;
using System.Collections.Generic;

namespace C7GameData.DataClasses
{
    // A specific instance of a unit on the map, belonging to some civ
    public class TileUnit
    {
        public Unit unitType;
        public Race race;
        public Civ civ;
        public Tile tile;
        public Tile previousTile;
        public Tile goToTile;
        public TileUnit loadedOn;
        public List<TileUnit> unitsLoaded;
    }
}
