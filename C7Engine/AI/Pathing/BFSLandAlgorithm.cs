using System.Collections.Generic;
using System.Linq;
using C7GameData;

namespace C7Engine.Pathing
{
	/**
	 * Uses the Breadth-First Search Algorithm to find a path between two tiles.
	 * Advantages: Simple.  Allows avoidance of obstacles such as water.
	 * Disadvantages: Does not weigh the tiles.  Thus it will have a proclivity
	 * to e.g. send units over the hills when they could take a road around them.
	 *
	 * Modifications: Use the predecessors structure to allow us to backtrack and
	 * find the path used for the shortest route, not just the distance.
	 */
	public class BFSLandAlgorithm : PathingAlgorithm
	{
		//N.B. This should really be static, but we can't put a static method on interfaces, so it isn't.
		public override TilePath PathFrom(Tile start, Tile destination)
		{
			if (start == destination) {
				return TilePath.EmptyPath(destination);
			}

			//Distance from start to a given tile
			Dictionary<Tile, int> distances = new Dictionary<Tile, int>();
			//Keeps track of which tile (value) preceded a tile (key), so we can reconstruct
			//the tiles once we know the shortest path.
			Dictionary<Tile, Tile> predecessors = new Dictionary<Tile, Tile>();
			Queue<Tile> tilesToVisit = new Queue<Tile>();
			distances[start] = 0;

			foreach(Tile tile in start.GetLandNeighbors()) {
				distances[tile] = 1;
				predecessors[tile] = start;
				tilesToVisit.Enqueue(tile);
			}

			//The core BFS algorithm.
			while (tilesToVisit.Count > 0) {
				Tile current = tilesToVisit.Dequeue();
				if (current == destination) {
					return ConstructPath(current, predecessors);
				}

				foreach (Tile tile in current.GetLandNeighbors()) {
					if (!distances.Keys.Contains(tile)) {
						distances[tile] = distances[current] + 1;
						predecessors[tile] = current;
						tilesToVisit.Enqueue(tile);
						if (tile == destination) {
							return ConstructPath(tile, predecessors);
						}
					}
				}
			}
			return TilePath.NONE;
		}
	}
}
