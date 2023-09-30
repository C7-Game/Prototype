using System;
using System.Collections.Generic;
using System.Threading;

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

		public void ComputeVisibleTiles(List<MapUnit> units) {
			visibleTiles.Clear();
			Console.WriteLine($"computing visible tiles based on {units.Count} units");
			foreach (MapUnit unit in units) {
				visibleTiles.Add(unit.location);
				foreach (Tile t in unit.location.neighbors.Values) {
					visibleTiles.Add(t);
				}
			}
			Console.WriteLine($"Visible tiles: {visibleTiles.Count}");
		}

		// neighboring tiles should not be added when loading tile knowledge
		// from a .sav file
		internal bool AddTileToKnown(Tile unitLocation) {
			return knownTiles.Add(unitLocation);
		}

		public bool isKnown(Tile t) {
			return knownTiles.Contains(t);
		}

		public bool isVisible(Tile t) {
			return visibleTiles.Contains(t);
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
