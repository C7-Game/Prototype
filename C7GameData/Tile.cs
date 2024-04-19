namespace C7GameData
{
	using System;
	using System.Text.Json.Serialization;
	using System.Collections.Generic;
	using System.Linq;
	public class Tile
	{
		public ID Id {get; internal set;}
		public Civ3ExtraInfo ExtraInfo;
		public int xCoordinate;
		public int yCoordinate;
		public string baseTerrainTypeKey { get; set; }
		[JsonIgnore]
		public TerrainType baseTerrainType = TerrainType.NONE;
		public string overlayTerrainTypeKey { get; set; }
		[JsonIgnore]
		public TerrainType overlayTerrainType = TerrainType.NONE;
		public City cityAtTile;
		[JsonIgnore]
		public bool HasCity => cityAtTile != null && cityAtTile != City.NONE;
		public CityResident personWorkingTile = null;	//allows us to see if another city is working this tile
		public bool hasBarbarianCamp = false;
		//One thing to decide is do we want to have a tile have a list of units on it,
		//or a unit have reference to the tile it is on, or both?
		//The downside of both is that both have to be updated (and it uses a miniscule amount
		//of memory for pointers), but I'm inclined to go with both since it makes it easy and
		//efficient to perform calculations, whether you need to know which unit on a tile
		//has the best defense, or which tile a unit is on when viewing the Military Advisor.
		public List<MapUnit> unitsOnTile = new List<MapUnit>();
		public string ResourceKey { get; set; }
		[JsonIgnore]
		public Resource Resource { get; set; }

		public Dictionary<TileDirection, Tile> neighbors { get; set; } = new Dictionary<TileDirection, Tile>();

		//See discussion on page 4 of the "Babylon" thread (https://forums.civfanatics.com/threads/0-1-babylon-progress-thread.673959) about sub-terrain type and Civ3 properties.
		//We may well move these properties somewhere, whether that's Civ3ExtraInfo, a Civ3Tile child class, a Dictionary property, or something else, in the future.
		public bool isBonusShield;
		public bool isSnowCapped;
		public bool isPineForest;

		public bool riverNorth;
		public bool riverNortheast;
		public bool riverEast;
		public bool riverSoutheast;
		public bool riverSouth;
		public bool riverSouthwest;
		public bool riverWest;
		public bool riverNorthwest;

		public TileOverlays overlays = new TileOverlays();

		public Tile(ID id)
		{
			this.Id = id;
			unitsOnTile = new List<MapUnit>();
			Resource = Resource.NONE;
		}

		internal Tile() {}

		// TODO: this should be either an extension in C7Engine, or otherwise
		// calculated somewhere else, but it's not obvious to someone unfamiliar
		// with the save format that it's the overaly terrain that has actual
		// movement cost
		public int MovementCost() {
			return overlayTerrainType.movementCost;
		}

		public static Tile NONE = new Tile(ID.None("tile")) {
			xCoordinate = -1,
			yCoordinate = -1,
		};

		//This should be used when we want to check if land tiles are next to water tiles.
		//Usually this is coast, but it could be Sea - see the "Deepwater Harbours" topics at CFC.
		//Sometimes we care *specifically* about the Coast terrain, e.g. galleys can only move on that terrain, not Sea or Ocean
		//Those cases should not use this method.
		public bool NeighborsWater() {
			foreach (Tile neighbor in neighbors.Values) {
				if (neighbor.baseTerrainType.isWater()) {
					return true;
				}
			}
			return false;
		}

		/// <summary>
		/// Returns neighbors along edges only.
		/// This is used by some graphics algorithms.
		/// </summary>
		/// <returns></returns>
		public Tile[] getEdgeNeighbors() {
			Tile[] edgeNeighbors =  { neighbors[TileDirection.NORTHEAST], neighbors[TileDirection.NORTHWEST], neighbors[TileDirection.SOUTHEAST], neighbors[TileDirection.SOUTHWEST]};
			return edgeNeighbors;
		}

		public override string ToString()
		{
			return "[" + xCoordinate + ", " + yCoordinate + "] (" + overlayTerrainType.DisplayName + " on " + baseTerrainType.DisplayName + ")";
		}

		public List<Tile> GetLandNeighbors() {
			return neighbors.Values.Where(tile => tile != NONE && !tile.baseTerrainType.isWater()).ToList();
		}

		/**
		 * Returns neighbors of the "Coast" type, not including Sea or Ocean.  This is used e.g. for Galley movement.
		 * Eventually, this should be refactored into a more general "get valid neighbors to move to" type of method,
		 * which could work e.g. for units that can move anywhere except desert.
		 **/
		public List<Tile> GetCoastNeighbors()
		{
			return neighbors.Values.Where(tile => tile.baseTerrainType.Key == "coast").ToList();
		}

		public bool HasRiverCrossing(TileDirection dir)
		{
			switch (dir) {
			case TileDirection.NORTH:     return riverNorth;
			case TileDirection.NORTHEAST: return riverNortheast;
			case TileDirection.EAST:      return riverEast;
			case TileDirection.SOUTHEAST: return riverSoutheast;
			case TileDirection.SOUTH:     return riverSouth;
			case TileDirection.SOUTHWEST: return riverSouthwest;
			case TileDirection.WEST:      return riverWest;
			case TileDirection.NORTHWEST: return riverNorthwest;
			default: throw new ArgumentOutOfRangeException("Invalid TileDirection");
			}
		}

		public bool IsLand()
		{
			return !baseTerrainType.isWater();
		}

		public bool IsWater() {
			return baseTerrainType.isWater();
		}

		public bool IsAllowCities() {
			return overlayTerrainType.allowCities;
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

			if      (angle >  7.0/8.0 * Math.PI) return TileDirection.WEST;
			else if (angle >  5.0/8.0 * Math.PI) return TileDirection.NORTHWEST;
			else if (angle >  3.0/8.0 * Math.PI) return TileDirection.NORTH;
			else if (angle >  1.0/8.0 * Math.PI) return TileDirection.NORTHEAST;
			else if (angle > -1.0/8.0 * Math.PI) return TileDirection.EAST;
			else if (angle > -3.0/8.0 * Math.PI) return TileDirection.SOUTHEAST;
			else if (angle > -5.0/8.0 * Math.PI) return TileDirection.SOUTH;
			else if (angle > -7.0/8.0 * Math.PI) return TileDirection.SOUTHWEST;
			else                                 return TileDirection.WEST;
		}

		/**
		 * Distance as the raven flies to another tile.
		 * This is a rough metric only.
		 */
		public int distanceTo(Tile other)
		{
			return (Math.Abs(other.xCoordinate - this.xCoordinate) + Math.Abs(other.yCoordinate - this.yCoordinate)) / 2;
		}

		public int foodYield(Player player)
		{
			int yield = overlayTerrainType.baseFoodProduction;
			if (this.Resource != Resource.NONE && player.KnowsAboutResource(Resource)) {
				yield += this.Resource.FoodBonus;
			}
			return yield;
		}

		public int productionYield(Player player)
		{
			int yield = overlayTerrainType.baseShieldProduction;
			if (overlayTerrainType.Key == "grassland" && this.isBonusShield) {
				yield++;
			}
			if (Resource != Resource.NONE && player.KnowsAboutResource(Resource)) {
				yield += this.Resource.ShieldsBonus;
			}
			return yield;
		}

		public int commerceYield(Player player)
		{
			int yield = overlayTerrainType.baseCommerceProduction;
			if (this.Resource != Resource.NONE && player.KnowsAboutResource(Resource)) {
				yield += this.Resource.CommerceBonus;
			}
			if (BordersRiver()) {
				yield += 1;
			}
			return yield;
		}

		private bool BordersRiver() {
			return riverNorth || riverNortheast || riverEast || riverSoutheast || riverSouth || riverSouthwest || riverWest || riverNorthwest;
		}

		//Convenience method for printing the yield
		public string YieldString(Player player) {
			return $"{foodYield(player)}/{productionYield(player)}/{commerceYield(player)})";
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
		NORTHWEST,
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

		// public static string shortName(this TileDirection dir) {
		// 	switch (dir) {
		// 	case TileDirection.NORTH:     return "N";
		// 	case TileDirection.NORTHEAST: return "NE";
		// 	case TileDirection.EAST:      return "E";
		// 	case TileDirection.SOUTHEAST: return "SE";
		// 	case TileDirection.SOUTH:     return "S";
		// 	case TileDirection.SOUTHWEST: return "SW";
		// 	case TileDirection.WEST:      return "W";
		// 	case TileDirection.NORTHWEST: return "NW";
		// 	default: throw new ArgumentOutOfRangeException("Invalid TileDirection");
		// 	}
		// }
	}

	public class TileOverlays {
		public bool road = false;
		public bool railroad = false;
		// assume that railroad contains road too
	}
}
