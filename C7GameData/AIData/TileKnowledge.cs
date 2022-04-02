using System.Collections.Generic;

namespace C7GameData
{
	public class TileKnowledge
	{
		HashSet<Tile> knownTiles = new HashSet<Tile>();
		HashSet<Tile> visibleTiles = new HashSet<Tile>();

		public void AddTilesToKnown(Tile unitLocation) {
			knownTiles.Add(unitLocation);
			foreach (Tile t in unitLocation.neighbors.Values) {
				knownTiles.Add(t);
			}
		}
	}
}