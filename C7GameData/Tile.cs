namespace C7GameData
{
    using System.Collections.Generic;
    public class Tile
    {
        // This will eventually be type object and use a type descriminator in JSON to determine
        //   how to deserialze. See https://docs.microsoft.com/en-us/dotnet/standard/serialization/system-text-json-polymorphism
        //   and https://docs.microsoft.com/en-us/dotnet/standard/serialization/system-text-json-converters-how-to?pivots=dotnet-5-0#support-polymorphic-deserialization
        public Civ3ExtraInfo ExtraInfo { get; set; }
        public int xCoordinate;
        public int yCoordinate;
        public TerrainType terrainType;
        public City cityAtTile;
        public bool hasBarbarianCamp = false;

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
