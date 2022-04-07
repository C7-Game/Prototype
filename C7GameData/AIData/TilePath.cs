using System.Collections.Generic;

namespace C7GameData
{
	public class TilePath
	{
		private Tile destination; //stored in case we need to re-calculate
		private Queue<Tile> path;

		private TilePath() {}

		public TilePath(Tile destination, Queue<Tile> path)
		{
			this.destination = destination;
			this.path = path;
		}

		public Tile Next()
		{
			return path.Dequeue();
		}

		//TODO: Once we have roads, we should return the calculated cost, not just the length.
		//This will require Dijkstra or another fancier pathing algorithm
		public int PathLength() {
			return path.Count;
		}

		//Indicates no path was found to the requested destination.
		public static TilePath NONE = new TilePath();
	}
}
