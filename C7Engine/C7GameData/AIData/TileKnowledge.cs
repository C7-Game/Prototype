using System.Collections.Generic;
using System.Linq;
using System;

namespace C7GameData {
	public class TileKnowledge {
		private readonly Player _player;

		public TileKnowledge(Player player) {
			_player = player;
		}

		public HashSet<Tile> knownTiles { get; private set; } = new();
		public HashSet<Tile> borderTiles { get; private set; } = new();
		private HashSet<Tile> activeTiles = new HashSet<Tile>();

		// Has this player explored all known ocean tiles?
		// TODO: this should be split out for coast/ocean
		public bool fullyExploredOceans = false;

		// The set of tiles this player currently has explorers headed towards.
		public HashSet<Tile> aiExplorationTargets = new();

		public void AddTilesToKnown(Tile unitLocation, bool recomputeActiveTiles = true) {
			knownTiles.Add(unitLocation);
			borderTiles.Remove(unitLocation);

			foreach (Tile t in GetTilesVisibleToUnit(unitLocation)) {
				knownTiles.Add(t);
				borderTiles.Remove(t);

				foreach (Tile border in t.neighbors.Values) {
					if (border == Tile.NONE) {
						continue;
					}
					if (!knownTiles.Contains(border)) {
						borderTiles.Add(border);
					}
				}
			}

			if (recomputeActiveTiles) {
				RecomputeActiveTiles();
			}
		}

		private HashSet<Tile> GetTilesVisibleToUnit(Tile unitLocation) {
			HashSet<Tile> result = new();
			result.Add(unitLocation);

			foreach (TileDirection a in TileDirection.GetValues(typeof(TileDirection))) {
				Tile innerRingNeighbor = unitLocation.neighbors[a];
				if (innerRingNeighbor == Tile.NONE) {
					continue;
				}
				result.Add(innerRingNeighbor);

				foreach (TileDirection b in TileDirection.GetValues(typeof(TileDirection))) {
					// Say we have the following. We are standing on the XX tile
					// and need to figure out which tiles we can see. We can see
					// the hill directly to our SW, because we're next to it.
					// We can see the hill that is S+SW of us, because it's
					// diagonal to us and we can see tiles of height 2 from 2
					// tiles away. We can't see the hill that is SW+SW of us
					// because we are blocked. To implement this we need to
					// prevent 90 degree "turns", which we handle by ensuring
					// the a+b combo is never a combination of two N/S/E/W dirs.
					//
					//                      <  ..  >
					//                  <  ..  ><  XX  >
					//              <  ..  >< Hill ><  gg  >
					//                  < Hill ><  gg  >
					//                      < Hill >
					if (((int)a) % 2 == 0 && ((int)b) % 2 == 0) {
						continue;
					}

					Tile outerRingNeighbor = innerRingNeighbor.neighbors[b];
					if (outerRingNeighbor == Tile.NONE) {
						continue;
					}
					int unitHeight = unitLocation.overlayTerrainType.height;
					int innerHeight = innerRingNeighbor.overlayTerrainType.height;
					int outerHeight = outerRingNeighbor.overlayTerrainType.height;

					// Tiles with a height of at least 2 are visible from 2 tiles
					// away as long as the tile in between is 2 less than the
					// outer tile (so you can see mountains over hills, but
					// not hills over forest).
					if (outerHeight >= 2 && innerHeight + 2 <= outerHeight) {
						result.Add(outerRingNeighbor);
						continue;
					}

					// You can also see tiles whose height is 2 lower than where
					// you are standing, as long as the tile in the middle has
					// height at least 2 lower than you.
					if (outerHeight + 1 <= unitHeight && innerHeight + 2 <= unitHeight) {
						result.Add(outerRingNeighbor);
						continue;
					}
				}
			}
			return result;
		}

		// neighboring tiles should not be added when loading tile knowledge
		// from a .sav file
		internal bool AddTileToKnown(Tile unitLocation) {
			bool added = knownTiles.Add(unitLocation);
			borderTiles.Remove(unitLocation);

			foreach (Tile border in unitLocation.neighbors.Values) {
				if (border == Tile.NONE) {
					continue;
				}
				if (!knownTiles.Contains(border)) {
					borderTiles.Add(border);
				}
			}

			return added;
		}

		public bool isTileKnown(Tile t) {
			return knownTiles.Contains(t);
		}

		public bool isActiveTile(Tile t) {
			if (t == Tile.NONE) {
				return false;
			}
			return activeTiles.Contains(t);
		}

		/**
		 * Returns a copy of the list of known tiles.
		 * This prevents external modifications.
		 **/
		public List<Tile> AllKnownTiles() {
			List<Tile> list = new List<Tile>();
			foreach (Tile t in knownTiles) {
				list.Add(t);
			}
			return list;
		}

		public void RecomputeActiveTiles() {
			activeTiles.Clear();
			foreach (Tile t in knownTiles) {
				// A tile within a city's borders and all of its neighbors are active.
				if (t.owningCity != null && t.owningCity.owner == _player) {
					activeTiles.Add(t);

					foreach (Tile neighbor in t.neighbors.Values) {
						activeTiles.Add(neighbor);
					}
					continue;
				}

				// A tile with a unit on it and all of its neighbors are active.
				if (t.unitsOnTile.Count > 0 && t.unitsOnTile[0].owner == _player) {
					foreach (Tile x in GetTilesVisibleToUnit(t)) {
						activeTiles.Add(x);
					}
				}
			}
		}
	}
}
