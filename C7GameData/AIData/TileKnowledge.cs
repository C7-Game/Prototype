using System.Collections.Generic;

namespace C7GameData
{
	public class TileKnowledge
	{
		HashSet<Tile> knownTiles = new HashSet<Tile>();
		HashSet<Tile> visibleTiles = new HashSet<Tile>();

		public bool AddTilesToKnown(Tile unitLocation) {
			bool added = knownTiles.Add(unitLocation);
			foreach (Tile t in unitLocation.neighbors.Values) {
				added |= knownTiles.Add(t);
			}
			return added;
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
