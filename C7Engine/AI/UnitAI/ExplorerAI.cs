using System;
using System.Collections.Generic;
using System.Linq;
using C7GameData;
using C7GameData.AIData;
using C7Engine.Pathing;

namespace C7Engine
{
	public class ExplorerAI : UnitAI
	{
		public bool PlayTurn(Player player, MapUnit unit)
		{
			ExplorerAIData explorerData = (ExplorerAIData)unit.currentAIData;
			if (MovingToNewExplorationArea(explorerData)) {
				return MoveToNextTileOnPath(explorerData, unit);
			}
			else {
				bool foundNeighboringTileToExplore = ExploreNeighboringTile(player, unit);
				if (foundNeighboringTileToExplore) {
					return true;
				}

				//Find the nearest tile that will allow us to continue exploring.
				//We prefer nearest because the one that allows the most discovery might be pretty far away
				bool foundNewPath = FindPathToNewExplorationArea(player, explorerData, unit);
				if (foundNewPath) {
					MoveToNextTileOnPath(explorerData, unit);
					return true;
				}
			}
			return false;
		}

		private static bool MoveToNextTileOnPath(ExplorerAIData explorerData, MapUnit unit) {
			Tile next = explorerData.path.Next();
			foreach (KeyValuePair<TileDirection, Tile> neighbor in unit.location.neighbors) {
				if (neighbor.Value == next) {
					unit.move(neighbor.Key);
					return true;
				}
			}
			//In the future, it might no longer be possible to go to the correct neighbor, perhaps
			//due to another civ's units having moved there.  Thus, this method can return false.
			return false;
		}

		private static bool ExploreNeighboringTile(Player player, MapUnit unit) {
			List<Tile> validNeighboringTiles = unit.unitType is SeaUnit ? unit.location.GetCoastNeighbors() : unit.location.GetLandNeighbors();
			if (validNeighboringTiles.Count == 0) {
				Console.WriteLine("No valid locations for unit " + unit + " at location " + unit.location);
				return false;
			}
			KeyValuePair<Tile, int> topScoringTile = FindTopScoringTileForExploration(player, validNeighboringTiles);
			Tile newLocation = topScoringTile.Key;

			if (newLocation != Tile.NONE && topScoringTile.Value > 0) {
				unit.move(unit.location.directionTo(newLocation));
				return true;
			}
			return false;
		}

		private static bool MovingToNewExplorationArea(ExplorerAIData explorerData) {
			return explorerData.path != null && explorerData.path.PathLength() > 0;
		}

		private static bool FindPathToNewExplorationArea(Player player, ExplorerAIData explorerData, MapUnit unit) {
			List<Tile> validExplorerTiles = new List<Tile>();
			foreach (Tile t in player.tileKnowledge.AllKnownTiles()
					.Where(t => unit.canTraverseTile(t) && t.cityAtTile == null && numUnknownNeighboringTiles(player, t) > 0))
			{
				validExplorerTiles.Add(t);
			}

			if (validExplorerTiles.Count == 0) {
				//Nowhere to explore.
				//TODO: Change unit AI behavior to something else e.g. defender
				return false;
			}

			int lowestDistance = int.MaxValue;
			TilePath chosenPath = null;

			PathingAlgorithm algo = PathingAlgorithmChooser.GetAlgorithm();
			foreach (Tile t in validExplorerTiles) {
				TilePath path = algo.PathFrom(unit.location, t);
				if (path.PathLength() < lowestDistance) {
					lowestDistance = path.PathLength();
					chosenPath = path;
				}
			}

			if (chosenPath == null) {
				//This could happen if there is e.g. a land tile that we could explore from, but on a different landmass.
				//Later, we might recruit a boat to take us there, but for now it's a fail state.
				return false;
			}
			explorerData.path = chosenPath;
			return true;
		}

		public static KeyValuePair<Tile, int> FindTopScoringTileForExploration(Player player, List<Tile> possibleNewLocations)
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
			//Do not try to explore a tile with a city.  If we own it, we know all tiles.
			//If someone else does, that would be war, which is not a scout's job.
			if (t.cityAtTile != null) {
				return 0;
			}
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