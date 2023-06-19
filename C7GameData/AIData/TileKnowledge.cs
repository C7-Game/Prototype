using System.Collections.Generic;

namespace C7GameData
{
	public class TileKnowledge
	{
		public HashSet<Tile> knownTiles = new HashSet<Tile>();
		public HashSet<Tile> visibleTiles = new HashSet<Tile>();

		public void AddTilesToKnown(Tile unitLocation) {
			knownTiles.Add(unitLocation);
			foreach (Tile t in unitLocation.neighbors.Values) {
				knownTiles.Add(t);
			}
		}

		public bool isTileKnown(Tile t) {
			return knownTiles.Contains(t);
		}

		/**
		 * Returns a copy of the list of known tiles.
		 * This prevents external modifications.
		 **/
		public List<Tile> AllKnownTiles()
		{
			List<Tile> list = new List<Tile>();
			foreach (Tile t in knownTiles) {
				list.Add(t);
			}
			return list;
		}
	}
}
