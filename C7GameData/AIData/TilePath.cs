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

		//Indicates no path was found to the requested destination.
		public static TilePath NONE = new TilePath();
	}
}
