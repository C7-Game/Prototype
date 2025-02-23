using System.Collections.Generic;
using System.Linq;

namespace C7Engine {
	using C7GameData;

	public static class TileExtensions {
		public static MapUnit FindTopDefender(this Tile tile, MapUnit opponent) {
			if (tile.unitsOnTile.Count > 0) {
				IEnumerable<MapUnit> potentialDefenders = tile.unitsOnTile.Where(u => u.CanDefendAgainst(opponent));
				if (potentialDefenders.Count() == 0) {
					return MapUnit.NONE;
				}

				MapUnit leadingCandidate = tile.unitsOnTile[0];
				foreach (MapUnit u in tile.unitsOnTile)
					if (u.HasPriorityAsDefender(leadingCandidate, opponent))
						leadingCandidate = u;
				return leadingCandidate;
			} else
				return MapUnit.NONE;
		}

		/// <summary>
		/// Disbands non-defending units on a tile.  This should only be called when all defending units have been destroyed,
		/// hence its name.  E.g. if only air/sea units remain after a land battle, this should be called.
		///
		/// Eventually, we should also have a method to make relevant units (workers, artillery, etc.) be captured.
		/// </summary>
		/// <param name="tile"></param>
		public static void DisbandNonDefendingUnits(this Tile tile) {
			//There may have been naval units, if so, disband them
			if (tile.unitsOnTile.Count > 0) {
				//Copy to a separate array so we don't crash due to concurrent modification exceptions
				MapUnit[] unitsOnTile = new MapUnit[tile.unitsOnTile.Count];
				tile.unitsOnTile.CopyTo(unitsOnTile);
				foreach (MapUnit destroyedUnit in unitsOnTile) {
					destroyedUnit.disband();
				}
			}
		}

		public static void Animate(this Tile tile, AnimatedEffect effect, bool wait) {
			if (EngineStorage.animationsEnabled) {
				new MsgStartEffectAnimation(tile, effect, wait ? EngineStorage.uiEvent : null, AnimationEnding.Stop).send();
				if (wait) {
					EngineStorage.gameDataMutex.ReleaseMutex();
					EngineStorage.uiEvent.WaitOne();
					EngineStorage.gameDataMutex.WaitOne();
				}
			}
		}
	}
}
