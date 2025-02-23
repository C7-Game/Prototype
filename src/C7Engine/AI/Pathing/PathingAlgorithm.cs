using System.Collections.Generic;
using System.Linq;
using C7GameData;

namespace C7Engine.Pathing {
	public abstract class PathingAlgorithm {
		public abstract TilePath PathFrom(Tile start, Tile destination);

		// Should not be public
		public TilePath ConstructPath(Tile destination, Dictionary<Tile, Tile> predecessors) {
			List<Tile> tilesInPath = new List<Tile>() {destination};
			Tile tile = destination;
			while (predecessors.ContainsKey(tile)) {
				tile = predecessors[tile];
				tilesInPath.Add(tile);
			}
			tilesInPath.Reverse();
			Queue<Tile> path = new Queue<Tile>();
			foreach (Tile t in tilesInPath.Skip(1)) {
				path.Enqueue(t);
			}
			return new TilePath(destination, path);
		}
	}

}
