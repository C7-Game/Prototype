namespace C7GameData
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
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

		public MapUnit findTopDefender(MapUnit opponent)
		{
			if (unitsOnTile.Count > 0) {
				var tr = unitsOnTile[0];
				foreach (var u in unitsOnTile)
					if (u.HasPriorityAsDefender(tr, opponent))
						tr = u;
				return tr;
			} else
				return MapUnit.NONE;
		}
		
		public static Tile NONE = new Tile();

		public bool NeighborsCoast() {
			foreach (Tile neighbor in getDiagonalNeighbors()) {
				if (neighbor.baseTerrainType.name == "Coast") {
					return true;
				}
			}
			return false;
		}

		public Tile[] getDiagonalNeighbors() {
			Tile[] diagonalNeighbors =  { neighbors[TileDirection.NORTHEAST], neighbors[TileDirection.NORTHWEST], neighbors[TileDirection.SOUTHEAST], neighbors[TileDirection.SOUTHWEST]};
			return diagonalNeighbors;
		}

		public override string ToString()
		{
			return "[" + xCoordinate + ", " + yCoordinate + "] (" + overlayTerrainType.name + " on " + baseTerrainType.name + ")";
		}

		public List<Tile> GetLandNeighbors() {
			return neighbors.Values.Where(tile => !tile.baseTerrainType.isWater()).ToList();
		}

		public List<Tile> GetCoastNeighbors()
		{
			return neighbors.Values.Where(tile => tile.baseTerrainType.name == "Coast").ToList();
		}

		public bool IsLand()
		{
			return !baseTerrainType.isWater();
		}

		public TileDirection directionTo(Tile other)
		{
			// TODO: Consider edge wrapping, the direction should point along the shortest path as the crow flies.

			if ((this == NONE) || (other == NONE))
				throw new System.Exception("Can't get direction toward NONE Tile since it doesn't have a meaningful location");

			// y calculation is reversed so dy is in typical Cartesian coords instead of tile coords, where y is inverted
			int dx = other.xCoordinate - this.xCoordinate;
			int dy = this.yCoordinate - other.yCoordinate;
			double angle = Math.Atan2(dy, dx); // angle is in interval [-pi, pi]

			if      (angle < -7.0/8.0 * Math.PI) return TileDirection.WEST;
			else if (angle < -5.0/8.0 * Math.PI) return TileDirection.NORTHWEST;
			else if (angle < -3.0/8.0 * Math.PI) return TileDirection.NORTH;
			else if (angle < -1.0/8.0 * Math.PI) return TileDirection.NORTHEAST;
			else if (angle <  1.0/8.0 * Math.PI) return TileDirection.EAST;
			else if (angle <  3.0/8.0 * Math.PI) return TileDirection.SOUTHEAST;
			else if (angle <  5.0/8.0 * Math.PI) return TileDirection.SOUTH;
			else if (angle <  7.0/8.0 * Math.PI) return TileDirection.SOUTHWEST;
			else                                 return TileDirection.WEST;
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

	public static class TileDirectionExtensions {
		public static TileDirection reversed(this TileDirection dir)
		{
			switch (dir) {
			case TileDirection.NORTH:     return TileDirection.SOUTH;
			case TileDirection.NORTHEAST: return TileDirection.SOUTHWEST;
			case TileDirection.EAST:      return TileDirection.WEST;
			case TileDirection.SOUTHEAST: return TileDirection.NORTHWEST;
			case TileDirection.SOUTH:     return TileDirection.NORTH;
			case TileDirection.SOUTHWEST: return TileDirection.NORTHEAST;
			case TileDirection.WEST:      return TileDirection.EAST;
			case TileDirection.NORTHWEST: return TileDirection.SOUTHEAST;
			default: throw new ArgumentOutOfRangeException("Invalid TileDirection");
			}
		}

		public static (int, int) toCoordDiff(this TileDirection dir)
		{
			switch (dir) {
			case TileDirection.NORTH:     return ( 0, -2);
			case TileDirection.NORTHEAST: return ( 1, -1);
			case TileDirection.EAST:      return ( 2,  0);
			case TileDirection.SOUTHEAST: return ( 1,  1);
			case TileDirection.SOUTH:     return ( 0,  2);
			case TileDirection.SOUTHWEST: return (-1,  1);
			case TileDirection.WEST:      return (-2,  0);
			case TileDirection.NORTHWEST: return (-1, -1);
			default: throw new ArgumentOutOfRangeException("Invalid TileDirection");
			}
		}
	}

}
