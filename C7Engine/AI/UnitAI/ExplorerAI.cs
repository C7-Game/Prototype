using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using C7GameData;
using C7GameData.AIData;
using C7Engine.Pathing;
using Serilog;

namespace C7Engine {
	public class ExplorerAI : C7GameData.UnitAI {
		private static ILogger log = Log.ForContext<ExplorerAI>();
		public ExplorerAIData data;

		public ExplorerAI(ExplorerAIData d) {
			data = d;
		}

		public static ExplorerAIData? MaybeMakeAiData(MapUnit unit, Player player) {
			HashSet<Tile> borderTiles = player.tileKnowledge.borderTiles;

			IEnumerable<Tile> candidates = borderTiles.Where(x => (x.IsLand() && unit.IsLandUnit()) || (!x.IsLand() && !unit.IsLandUnit()));
			ExplorerAIData? result = PickBestTileToExplore(unit, CalculateExplorationScores(player, unit, candidates));

			if (result == null) {
				if (!unit.IsLandUnit()) {
					player.tileKnowledge.fullyExploredOceans = true;
				}

				return result;
			}

			log.Information($"Set AI for unit {unit.id} at {unit.location} to explore with destination of " + result.destination);
			player.tileKnowledge.aiExplorationTargets.Add(result.destination);
			return result;
		}

		UnitAI.MoveResult UnitAI.PlayTurnImpl(Player player, MapUnit unit) {
			if (data == null) {
				return UnitAI.Result.Error;
			}

			if (player.tileKnowledge.isTileKnown(data.destination)) {
				player.tileKnowledge.aiExplorationTargets.Remove(data.destination);
				return UnitAI.Result.Done;
			}

			// If we're at the destination we're done.
			if (unit.location == data.destination) {
				player.tileKnowledge.aiExplorationTargets.Remove(data.destination);
				return UnitAI.Result.Done;
			}

			return this.TryToMoveAlongPath(unit, ref data.pathToDestination, allowCombat: false);
		}

		public string SummarizePlan() {
			return "ExplorerAI: " + data.ToString();
		}

		public void UpdateOnDeath() { }

		private static int DistanceToNearestCity(Player player, Tile t) {
			int result = int.MaxValue;
			foreach (City c in player.cities) {
				int distance = t.distanceTo(c.location);
				if (distance < result) {
					result = distance;
				}
			}
			return result;
		}

		private static int DistanceToNearestExplorationTarget(Player player, Tile t) {
			int result = int.MaxValue;
			foreach (Tile target in player.tileKnowledge.aiExplorationTargets) {
				int distance = t.distanceTo(target);
				if (distance < result) {
					result = distance;
				}
			}
			return result;
		}

		private static Dictionary<Tile, float> CalculateExplorationScores(Player player, MapUnit unit, IEnumerable<Tile> possibleNewLocations) {
			Dictionary<Tile, float> explorationScores = new Dictionary<Tile, float>();
			foreach (Tile t in possibleNewLocations) {
				int numUnknownNeighbors = numUnknownNeighboringTiles(player, t);

				// Don't waste time on uninteresting tiles.
				if (numUnknownNeighbors == 0) {
					continue;
				}

				// Don't waste time on tiles already being explored.
				if (player.tileKnowledge.aiExplorationTargets.Contains(t)) {
					continue;
				}

				// Give a large score to tiles that we don't know much about.
				int score = numUnknownNeighbors;
				score *= numUnknownNeighbors;

				// But penalize tiles that are far away from our cities, to
				// encourate semi-local exploration. Without this we won't know
				// city sites.
				if (player.cities.Count > 0) {
					score -= DistanceToNearestCity(player, t);
				}

				// Similarly with tiles that are far away from us. We use
				// distanceTo as a quick heuristic to avoid expensive
				// pathfinding.
				score -= unit.location.distanceTo(t);

				// Finally, reward tiles that are far away from tiles already
				// being explored by other explorers to encourage exploring in
				// different directions.
				if (player.tileKnowledge.aiExplorationTargets.Count > 0) {
					score += DistanceToNearestExplorationTarget(player, t);
				}

				explorationScores[t] = score;
			}

			return explorationScores;
		}

		private static ExplorerAIData? PickBestTileToExplore(MapUnit unit, Dictionary<Tile, float> explorationScores) {
			if (explorationScores.Count == 0) {
				return null;
			}

			PathingAlgorithm algorithm = PathingAlgorithmChooser.GetAlgorithm(unit);

			IOrderedEnumerable<KeyValuePair<Tile, float>> orderedScores = explorationScores.OrderByDescending(t => t.Value);
			foreach (KeyValuePair<Tile, float> p in orderedScores) {
				ExplorerAIData result = new ();
				result.destination = p.Key;
				result.pathToDestination = algorithm.PathFrom(unit.location, result.destination, unit);

				// If we can't reach the destination, go to the next candidate.
				if ((result.pathToDestination?.PathLength() ?? -1) == -1) {
					continue;
				}

				return result;
			}
			return null;
		}

		private static int numUnknownNeighboringTiles(Player player, Tile t) {
			//Do not try to explore a tile with a city.  If we own it, we know all tiles.
			//If someone else does, that would be war, which is not a scout's job.
			if (t.cityAtTile != null) {
				return 0;
			}
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
			return discoverableTiles;
		}
	}
}
