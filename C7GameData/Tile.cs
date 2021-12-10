namespace C7GameData
{
    using System.Collections.Generic;
    public class Tile
    {
        // ExtraInfo will eventually be type object and use a type descriminator in JSON to determine
        //   how to deserialze. See https://docs.microsoft.com/en-us/dotnet/standard/serialization/system-text-json-polymorphism
        //   and https://docs.microsoft.com/en-us/dotnet/standard/serialization/system-text-json-converters-how-to?pivots=dotnet-5-0#support-polymorphic-deserialization
        //   Needed for saving to and loading from a serializable format
        public Civ3ExtraInfo ExtraInfo;
        public int xCoordinate;
        public int yCoordinate;
        public TerrainType baseTerrainType = TerrainType.NONE;
        public TerrainType overlayTerrainType = TerrainType.NONE;
        public City cityAtTile;
        public bool hasBarbarianCamp = false;

        //One thing to decide is do we want to have a tile have a list of units on it,
        //or a unit have reference to the tile it is on, or both?
        //The downside of both is that both have to be updated (and it uses a miniscule amount
        //of memory for pointers), but I'm inclined to go with both since it makes it easy and
        //efficient to perform calculations, whether you need to know which unit on a tile
        //has the best defense, or which tile a unit is on when viewing the Military Advisor.
        public List<MapUnit> unitsOnTile;

        public Dictionary<TileDirection, Tile> neighbors {get; set;}

        //See discussion on page 4 of the "Babylon" thread (https://forums.civfanatics.com/threads/0-1-babylon-progress-thread.673959) about sub-terrain type and Civ3 properties.
        //We may well move these properties somewhere, whether that's Civ3ExtraInfo, a Civ3Tile child class, a Dictionary property, or something else, in the future.
        public bool isSnowCapped;
        public bool isPineForest;

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
        
        public static Tile NONE = new Tile();

        public override string ToString()
        {
            return "[" + xCoordinate + ", " + yCoordinate + "] (" + overlayTerrainType.name + " on " + baseTerrainType.name + ")";
        }
    }

    public enum TileDirection {
        NORTH,
        NORTHEAST,
        EAST,
        SOUTHEAST,
        SOUTH,
        SOUTHWEST,
        WEST,
        NORTHWEST
    }
}
