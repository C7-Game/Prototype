using System;
using System.Collections.Generic;
using System.Linq;
using C7GameData;

namespace C7Engine.Pathing
{
	/**
	 * Uses Dijkstra's Algorithm to find a path between two tiles.
	 * Advantages: Finds the shortest path accounting for tile movement cost
	 * Disadvantages: Expensive, will exhaustively search every tile on the continent
	 *
	 * Notes: - Edge weight is defined by the movement cost of the 2nd node:
	 *          w(u, v) = movement_cost(v)
	 */
	public class DijkstrasLandAlgorithm : PathingAlgorithm {

		// Returns the next closest vertex in the graph, and updates the visited set to include
		// the returned vertex
		private KeyValuePair<Tile, int> getNextClosest(Dictionary<Tile, int> dist, HashSet<Tile> visited) {
			// TODO: pick a different data structure to avoid O(n) search
			KeyValuePair<Tile, int> next = new KeyValuePair<Tile, int>(Tile.NONE, int.MaxValue);
			foreach (KeyValuePair<Tile, int> pair in dist) {
				if (pair.Value < next.Value && !visited.Contains(pair.Key)) {
					next = pair;
				}
			}
			visited.Add(next.Key);
			return next;
		}

		// Updates the shortest distance to a vertex. Returns true if the distance parameter
		// is shorter than the shortest known distance and dist is updated, and false otherwise.
		private bool updateShortestDistance(Tile tile, int distance, Dictionary<Tile, int> dist) {
			if (!dist.ContainsKey(tile) || dist[tile] > distance) {
				dist[tile] = distance;
				return true;
			}
			return false;
		}

		public override TilePath PathFrom(Tile start, Tile destination) {
			if (!destination.IsLand()) {
				return TilePath.NONE;
			}

			// shortest distance from start to each tile on the continent
			Dictionary<Tile, int> dist = new Dictionary<Tile, int>();
			Dictionary<Tile, Tile> predecessors = new Dictionary<Tile, Tile>();
			HashSet<Tile> visited = new HashSet<Tile>();

			dist[start] = 0;

			KeyValuePair<Tile, int> closest = getNextClosest(dist, visited);
			while (closest.Key != Tile.NONE) {
				foreach (Tile tile in closest.Key.GetLandNeighbors()) {
					if (!visited.Contains(tile) && updateShortestDistance(tile, closest.Value + tile.MovementCost(), dist)) {
						predecessors[tile] = closest.Key;
					}
				}
				closest = getNextClosest(dist, visited);
				// TODO: this is fastest if recomputing Dijkstra's for each unit
				// and ignoring that units may path from the same starting tile
				if (closest.Key == destination) {
					break;
				}
			}
			return ConstructPath(destination, predecessors);
		}
	}
}
