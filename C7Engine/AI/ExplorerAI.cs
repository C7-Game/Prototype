using System;
using System.Collections.Generic;
using System.Linq;
using C7GameData;
using C7GameData.AIData;

namespace C7Engine
{
	public class ExplorerAI
	{
		public static void PlayExplorerTurn(Player player, ExplorerAIData explorerData, MapUnit unit)
		{
			// Console.Write("Moving explorer AI for " + unit);
			//TODO: Distinguish between types of exploration
			//TODO: Make sure ON_A_BOAT units stay on the boat
			//Move randomly
			List<Tile> possibleNewLocations = unit.unitType is SeaUnit ? unit.location.GetCoastNeighbors() : unit.location.GetLandNeighbors();
			if (possibleNewLocations.Count == 0) {
				Console.WriteLine("No valid locations for unit " + unit + " at location " + unit.location);
				return;
			}
			//Technically, this should be the *estimated* new tiles revealed.  If a mountain blocks visibility,
			//we won't know that till we move there.
			Dictionary<Tile, int> numNewTilesRevealed = new Dictionary<Tile, int>();
			foreach (Tile t in possibleNewLocations) {
				//Calculate whether it, and its neighbors are in known tiles.
				int discoverableTiles = 0;
				if (!player.tileKnowledge.isTileKnown(t)) {
					discoverableTiles++;
				}
				foreach (Tile n in t.neighbors.Values) {
					if (!player.tileKnowledge.isTileKnown(n)) {
						discoverableTiles++;
					}
				}
				numNewTilesRevealed[t] = discoverableTiles;
			}
			IOrderedEnumerable<KeyValuePair<Tile, int> > orderedScores = numNewTilesRevealed.OrderByDescending(t => t.Value);
			Tile newLocation = orderedScores.First().Key;

			//Because it chooses a semi-cardinal direction at random, not accounting for map, it could get none
			//if it tries to move e.g. north from the north pole.  Hence, this check.
			if (newLocation != Tile.NONE) {
				// Console.WriteLine("Moving unit at " + unit.location + " to " + newLocation);
				unit.move(unit.location.directionTo(newLocation));
			}
		}
	}
}