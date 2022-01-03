using System;
using System.Collections.Generic;

namespace C7GameData.DataClasses
{
    // A tile on a map
    public class Tile
    {
        public Continent continent;
        public CityCitizen cityCitizen;
        public Terrain terrain;
        public List<Overlay> overlays;
        public City city;
        public List<CivTile> civTile;
        public Map map;
        public Resource resource;
        public List<TileUnit> unitsOnTile;
        public List<TileUnit> unitsGoingToTile;
        public List<TileUnit> unitsPreviouslyOnTile;
    }
}
