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
	 */
	public class BFSLandAlgorithm : PathingAlgorithm
	{
		//N.B. This should really be static, but we can't put a static method on interfaces, so it isn't.
		public TilePath PathFrom(Tile start, Tile destination)
		{
			//Distance from start to a given tile
			Dictionary<Tile, int> distances = new Dictionary<Tile, int>();
			//Keeps track of which tile (value) preceded a tile (key), so we can reconstruct
			//the tiles once we know the shortest path.
			Dictionary<Tile, Tile> predecessors = new Dictionary<Tile, Tile>();
			HashSet<Tile> visitedTiles = new HashSet<Tile>();
			visitedTiles.Add(start);
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
				visitedTiles.Add(current);
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

		/**
		 * Should not be public.  Only public so we can test
		 * it in isolation.
		 *
		 * In Java, I could work around this a few ways... not sure how in C#.
		 */
		public static TilePath ConstructPath(Tile destination, Dictionary<Tile, Tile> predecessors)
		{
			List<Tile> tilesInPath = new List<Tile>();
			tilesInPath.Add(destination);
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
