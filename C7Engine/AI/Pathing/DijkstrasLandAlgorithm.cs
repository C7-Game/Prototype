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
	public class DijkstrasLandAlgorithm : PathingAlgorithm
	{
		//N.B. This should really be static, but we can't put a static method on interfaces, so it isn't.
		public TilePath PathFrom(Tile start, Tile destination)
		{
			// shortest distance from start to each tile on the continent
			Dictionary<Tile, int> dist = new Dictionary<Tile, int>();
			Dictionary<Tile, Tile> predecessors = new Dictionary<Tile, Tile>();
			HashSet<Tile> visited = new HashSet<Tile>();

			dist[start] = 0;

			bool updateShortestDistance(Tile tile, int distance) {
				if (!dist.ContainsKey(tile)) {
					dist[tile] = distance;
					return true;
				} else if (dist[tile] > distance) {
					dist[tile] = distance;
					return true;
				}
				return false;
			}

			// TODO not O(n) search
			KeyValuePair<Tile, int> getNextClosest() {
				KeyValuePair<Tile, int> next = new KeyValuePair<Tile, int>(Tile.NONE, int.MaxValue);
				foreach (KeyValuePair<Tile, int> pair in dist) {
					if (pair.Value < next.Value && !visited.Contains(pair.Key)) {
						next = pair;
					}
				}
				visited.Add(next.Key);
				return next;
			}

			KeyValuePair<Tile, int> closest = getNextClosest();
			while (closest.Key != Tile.NONE) {
				foreach (Tile tile in closest.Key.GetLandNeighbors()) {
					if (!visited.Contains(tile) && updateShortestDistance(tile, closest.Value + tile.MovementCost())) {
						predecessors[tile] = closest.Key;
					}
				}
				closest = getNextClosest();
				// TODO: this is fastest if recomputing Dijkstra's for each unit
				// and ignoring that units may path from the same starting tile
				if (closest.Key == destination) {
					break;
				}
			}

			return ConstructPath(destination, predecessors);
		}

		// Should not be public
		public static TilePath ConstructPath(Tile destination, Dictionary<Tile, Tile> predecessors)
		{
			List<Tile> tilesInPath = new List<Tile>() {destination};
			Tile tile = destination;
			while (predecessors.ContainsKey(tile)) {
				tile = predecessors[tile];
				tilesInPath.Add(tile);
			}
			tilesInPath.Reverse();
			Queue<Tile> path = new Queue<Tile>();
			foreach (Tile t in tilesInPath.Skip(1))
			{
				path.Enqueue(t);
			}
			return new TilePath(destination, path);
		}
	}
}
