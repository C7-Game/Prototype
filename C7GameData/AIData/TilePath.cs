using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace C7GameData {
	public class TilePath {
		private Tile destination; //stored in case we need to re-calculate
		public Queue<Tile> path { get; private set; }

		private TilePath() {
			destination = Tile.NONE;
			path = new Queue<Tile>();
		}

		public TilePath(Tile destination, Queue<Tile> path) {
			this.destination = destination;
			this.path = path;
		}

		// The next tile in the path, or Tile.NONE if there
		// are no remaining tiles, or the path is invalid
		public Tile Next() {
			return PathLength() > 0 ? path.Dequeue() : Tile.NONE;
		}

		//TODO: Once we have roads, we should return the calculated cost, not just the length.
		//This will require Dijkstra or another fancier pathing algorithm
		public int PathLength() {
			return path != null ? path.Count : -1;
		}

		public int PathCost(Tile from, float perTurnMovePoints, float remainingMovementPoints) {
			// If we have no path (such as if we are a land unit trying to move to the water)
			// return -1 so we don't display a goto cursor.
			if (path == null || path.Count == 0) { return -1; }

			int turns = 0;

			// Start out with however many movement points the unit has left.
			MovementPoints movementPoints = new();
			movementPoints.reset(remainingMovementPoints);

			foreach (Tile tile in path) {
				// Subtract the cost of the next move.
				float cost = getMovementCost(from, from.directionTo(tile), tile);
				movementPoints.onUnitMove(cost);

				// If we can't do any more moves, bump up the turn cost and reset
				// our MP to the base value.
				if (!movementPoints.canMove) {
					++turns;
					movementPoints.reset(perTurnMovePoints);
				}

				from = tile;
			}

			// Special case: if we consumed part of our movement points (such as by
			// walking along a road, consuming 1/3 of a point), round up the cost.
			// This prevents showing 0 turns when moving one tile along a road, or
			// 1 turn when moving 4 tiles along a road.
			if (movementPoints.remaining < (turns == 0 ? remainingMovementPoints : perTurnMovePoints)) {
				++turns;
			}

			return turns;
		}

		public HashSet<Vector2> GetPathCoords() {
			HashSet<Vector2> result = new();

			foreach (Tile tile in path) {
				result.Add(new Vector2(tile.XCoordinate, tile.YCoordinate));
			}

			return result;
		}

		// Indicates no path was found to the requested destination.
		public static TilePath NONE = new TilePath();

		// A valid path of length 0
		public static TilePath EmptyPath(Tile destination) {
			return new TilePath(destination, new Queue<Tile>());
		}

		public static float getMovementCost(Tile from, TileDirection dir, Tile newLocation) {
			// River crossings disrupt roads, so check that first.
			if (from.HasRiverCrossing(dir)) return newLocation.MovementCost();

			// Travelling between two tiles with railroads is free.
			if (from.overlays.railroad && newLocation.overlays.railroad) return 0;

			// Traveling from a railroad/road to a road has the cost of a road; 1/3.
			if ((from.overlays.railroad || from.overlays.road) && newLocation.overlays.road) return 1.0f / 3;

			// Special case: if we are a water unit, traveling from the water into
			// a city, it doesn't matter if the city is on hills or on grassland,
			// the cost should always be 1.
			if (from.IsWater() && newLocation.HasCity) return 1;

			return newLocation.MovementCost();
		}
	}
}
