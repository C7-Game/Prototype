using System.Collections.Generic;
using System.Linq;
using System;
using C7GameData;

namespace C7Engine.Pathing {
	public class TileAndCost : IComparable<TileAndCost> {
		public Tile tile;
		public double cost = 0;

		public int CompareTo(TileAndCost other) {
			return cost.CompareTo(other.cost);
		}
	}

	// An implementation fo the A* path searching algorithm.
	//
	// See https://en.wikipedia.org/wiki/A*_search_algorithm and
	// https://github.com/yairm210/Unciv/blob/master/core/src/com/unciv/logic/map/AStar.kt
	// for some useful details.
	public class AStarAlgorithm : PathingAlgorithm {
		private readonly EdgeWalker<Tile> edgeWalker;

		// An estimate of the cost between two tiles, usually between a tile in
		// the search and the destination tile. This allows targeting the search
		// by doing things like focusing the search in the cardinal direction of
		// the destination instead of expanding outwards in all directions.
		private Func<Tile, Tile, double> costHeuristic;

		public AStarAlgorithm(EdgeWalker<Tile> edgeWalker, Func<Tile, Tile, double> costHeuristic) {
			this.edgeWalker = edgeWalker;
			this.costHeuristic = costHeuristic;
		}

		public override TilePath PathFrom(Tile start, Tile destination) {
			// The set of tiles to explore next, ordered by their cumulative cost
			// so far and the estimate of the cost to the goal.
			BinaryMinHeap<TileAndCost> openSet = new();
			openSet.insert(new TileAndCost() {
				tile = start,
				cost = 0,
			});

			// For a given tile, cameFrom[tile] is the tile preceeding it on the
			// cheapest path from start->tile.
			Dictionary<Tile, Tile> cameFrom = new();

			// For a given tile, knownCosts[tile] is the currently known cheapest
			// path from start->tile.
			Dictionary<Tile, double> knownCosts = new();
			knownCosts.Add(start, 0);

			while (openSet.count > 0) {
				// Get the tile with the lowest estimated cost.
				TileAndCost currentTileAndCost = openSet.extract();
				Tile current = currentTileAndCost.tile;

				// If this is the destination, we're done.
				if (current == destination) {
					return makePath(cameFrom, current);
				}

				// Otherwise check each neighbor that we can use.
				foreach (Edge<Tile> neighborEdge in edgeWalker.getEdges(current)) {
					Tile neighbor = neighborEdge.current;
					double newCost = neighborEdge.distanceToCurrent + knownCosts[current];

					// If we didn't know the cost to get to this neighbor, or 
					// this new path is now cheaper, update our data structures.
					if (!knownCosts.ContainsKey(neighbor) || newCost < knownCosts[neighbor]) {
						UpdateDictionary(knownCosts, neighbor, newCost);
						double estimatedCost = newCost + costHeuristic(neighbor, destination);
						openSet.insert(new TileAndCost() {
							tile = neighbor,
							cost = estimatedCost,
						});
						UpdateDictionary(cameFrom, neighbor, current);
					}
				}
			}

			return TilePath.EmptyPath(destination);
		}

		private static TilePath makePath(Dictionary<Tile, Tile> cameFrom, Tile current) {
			List<Tile> tilesInPath = new List<Tile>() { current };
			Tile tile = current;
			while (cameFrom.ContainsKey(tile)) {
				tile = cameFrom[tile];
				tilesInPath.Add(tile);
			}

			tilesInPath.Reverse();
			Queue<Tile> path = new Queue<Tile>();
			foreach (Tile t in tilesInPath.Skip(1)) {
				path.Enqueue(t);
			}
			return new TilePath(current, path);
		}

		private static void UpdateDictionary<Key, Value>(Dictionary<Key, Value> dictionary, Key key, Value value) {
			if (dictionary.ContainsKey(key)) {
				dictionary[key] = value;
			} else {
				dictionary.Add(key, value);
			}
		}
	}
}
