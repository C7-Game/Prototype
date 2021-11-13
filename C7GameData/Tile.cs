namespace C7GameData
{
    using System.Collections.Generic;
    public class Tile
    {
        public int xCoordinate;
        public int yCoordinate;
        public TerrainType terrainType;

        //One thing to decide is do we want to have a tile have a list of units on it,
        //or a unit have reference to the tile it is on, or both?
        //The downside of both is that both have to be updated (and it uses a miniscule amount
        //of memory for pointers), but I'm inclined to go with both since it makes it easy and
        //efficient to perform calculations, whether you need to know which unit on a tile
        //has the best defense, or which tile a unit is on when viewing the Military Advisor.
        public List<MapUnit> unitsOnTile;

        public Tile()
        {
            unitsOnTile = new List<MapUnit>();
        }

	    public MapUnit findTopDefender()
	    {
		    if (unitsOnTile.Count > 0) {
			    var tr = unitsOnTile[0];
			    foreach (var u in unitsOnTile)
				    if (u.unitType.defense * u.hitPointsRemaining > tr.unitType.defense * tr.hitPointsRemaining)
					    tr = u;
			    return tr;
		    } else
			    return MapUnit.NONE;
	    }
    }
}
