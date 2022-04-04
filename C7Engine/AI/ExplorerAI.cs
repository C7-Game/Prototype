using System;
using System.Collections.Generic;
using System.Linq;
using C7GameData;
using C7GameData.AIData;
using C7Engine.Pathing;

namespace C7Engine
{
	public class ExplorerAI
	{
		public static async void PlayExplorerTurn(Player player, ExplorerAIData explorerData, MapUnit unit)
		{
			// Console.Write("Moving explorer AI for " + unit);
			if (explorerData.path != null && explorerData.path.PathLength() > 0) {
				Tile next = explorerData.path.Next();
				foreach (KeyValuePair<TileDirection, Tile> neighbor in unit.location.neighbors) {
					if (neighbor.Value == next) {
						unit.move(neighbor.Key);
						return;
					}
				}
			}

			List<Tile> validNeighboringTiles = unit.unitType is SeaUnit ? unit.location.GetCoastNeighbors() : unit.location.GetLandNeighbors();
			if (validNeighboringTiles.Count == 0)
			{
				Console.WriteLine("No valid locations for unit " + unit + " at location " + unit.location);
				return;
			}
			KeyValuePair<Tile, int> topScoringTile = FindTopScoringTile(player, validNeighboringTiles);
			Tile newLocation = topScoringTile.Key;

			//Because it chooses a semi-cardinal direction at random, not accounting for map, it could get none
			//if it tries to move e.g. north from the north pole.  Hence, this check.
			if (newLocation != Tile.NONE && topScoringTile.Value > 0)
			{
				// Console.WriteLine("Moving unit at " + unit.location + " to " + newLocation);
				unit.move(unit.location.directionTo(newLocation));
			}
			else if (newLocation != Tile.NONE)
			{
				//Find the nearest tile that will allow us to continue exploring.
				//We prefer nearest because the one that allows the most discovery might be pretty far away
				List<Tile> validExplorerTiles = new List<Tile>();
				foreach (Tile t in player.tileKnowledge.AllKnownTiles()
						.Where(t => unit.canTraverseTile(t) && numUnknownNeighboringTiles(player, t) > 0))
				{
					validExplorerTiles.Add(t);
				}

				if (validExplorerTiles.Count == 0) {
					//TODO: Change unit AI behavior to something else e.g. defender
					return;
				}

				int lowestDistance = int.MaxValue;
				Tile nearestTile = Tile.NONE;
				TilePath chosenPath = null;

				PathingAlgorithm algo = PathingAlgorithmChooser.GetAlgorithm();
				foreach (Tile t in validExplorerTiles) {
					TilePath path = algo.PathFrom(unit.location, t);
					if (path.PathLength() < lowestDistance) {
						lowestDistance = path.PathLength();
						nearestTile = t;
						chosenPath = path;
					}
				}

				if (nearestTile != Tile.NONE) {
					explorerData.path = chosenPath;
				}
			}
		}

		private static KeyValuePair<Tile, int> FindTopScoringTile(Player player, List<Tile> possibleNewLocations)
		{
			//Technically, this should be the *estimated* new tiles revealed.  If a mountain blocks visibility,
			//we won't know that till we move there.
			Dictionary<Tile, int> numNewTilesRevealed = new Dictionary<Tile, int>();
			foreach (Tile t in possibleNewLocations)
			{
				numNewTilesRevealed[t] = numUnknownNeighboringTiles(player, t);
			}
			IOrderedEnumerable<KeyValuePair<Tile, int>> orderedScores = numNewTilesRevealed.OrderByDescending(t => t.Value);
			return orderedScores.First();
		}

		private static int numUnknownNeighboringTiles(Player player, Tile t) {
			//Calculate whether it, and its neighbors are in known tiles.
			int discoverableTiles = 0;
			if (!player.tileKnowledge.isTileKnown(t))
			{
				discoverableTiles++;
			}
			foreach (Tile n in t.neighbors.Values)
			{
				if (!player.tileKnowledge.isTileKnown(n))
				{
					discoverableTiles++;
				}
			}
			return discoverableTiles;
		}
	}
}