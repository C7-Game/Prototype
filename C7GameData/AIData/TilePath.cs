using System.Collections.Generic;

namespace C7GameData
{
	public class TilePath
	{
		private Tile destination; //stored in case we need to re-calculate
		public Queue<Tile> path {get; private set;}

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

		// Indicates no path was found to the requested destination.
		public static TilePath NONE = new TilePath();

		// A valid path of length 0
		public static TilePath EmptyPath(Tile destination) {
			return new TilePath(destination, new Queue<Tile>());
		}

	}
}
