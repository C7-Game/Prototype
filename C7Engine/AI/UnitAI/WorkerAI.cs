using C7Engine.Pathing;
using C7GameData;
using System.Collections.Generic;
using System.Linq;
using C7GameData.AIData;
using C7Engine.AI;
using Serilog;

namespace C7Engine {
	public class WorkerAI : UnitAI {
		private static ILogger log = Log.ForContext<WorkerAI>();
		private WorkerAIData data;

		public WorkerAI(WorkerAIData d) {
			data = d;
		}

		public static WorkerAIData MakeAiData(MapUnit unit, Player player) {
			// First, check if there are any tiles our civ works that we can
			// improve.
			List<Tile> highPriorityTiles = new();
			foreach (City city in player.cities) {
				foreach (CityResident cr in city.residents) {
					if (cr.tileWorked != Tile.NONE) {
						highPriorityTiles.Add(cr.tileWorked);
					}
				}
			}

			// Owned tiles that aren't worked are lower priority, but if they
			// have a resource they're a higher priority.
			List<Tile> lowPriorityTiles = new();
			foreach (City city in player.cities) {
				foreach (Tile t in city.GetTilesWithinBorders()) {
					if ((t.Resource.Category == ResourceCategory.LUXURY || t.Resource.Category == ResourceCategory.STRATEGIC)
						&& player.KnowsAboutResource(t.Resource)) {
						highPriorityTiles.Add(t);
					} else {
						lowPriorityTiles.Add(t);
					}
				}
			}

			WorkerAIData? result = GetPlanToImproveNearestUnimproved(unit, player, highPriorityTiles);
			if (result != null) {
				return result;
			}

			// If all our high priority tiles are improved, improve the lower
			// priority tiles.
			return GetPlanToImproveNearestUnimproved(unit, player, lowPriorityTiles);
		}

		public string SummarizePlan() {
			return "WorkerAI: " + data.ToString();
		}

		public void UpdateOnDeath() { }

		UnitAI.MoveResult UnitAI.PlayTurnImpl(Player player, MapUnit unit) {
			if (data == null) {
				return UnitAI.Result.Error;
			}

			// If we're at the destination, do our planned move.
			if (unit.location == data.destination) {
				return PerformWorkerMove(unit, data.workerMove);
			}

			// Otherwise we're moving towards our actual goal. To avoid wasting
			// worker moves, see if there's anything we can do on this tile
			// before moving to the next.
			//
			// This also functions as a way to build a road network, since we
			// will eventually road between all worked tiles of all cities.
			Terraform? improvement = GetTileImprovement(unit.location, unit);
			if (improvement != null) {
				return PerformWorkerMove(unit, improvement);
			}

			return this.TryToMoveAlongPath(unit, ref data.pathToDestination, TileProbe.MoveNonAggroProbe());
		}

		private static Terraform? GetTileImprovement(Tile t, MapUnit unit) {
			if (!t.IsLand()) {
				return null;
			}

			HashSet<Terraform> accessibleTerraforms = EngineStorage.gameData.Terraforms
													.Where(terr => unit.canPerformTerraformAction(terr, t))
													.ToHashSet();

			if (accessibleTerraforms.Count == 0) {
				return null;
			}

			Player player = unit.owner;

			// Don't waste worker moves on unowned tiles. We do allow roading
			// unowned tiles though.
			if (t.owningCity == null || t.owningCity.owner != player) {
				var buildRoad = accessibleTerraforms.FirstOrDefault(t => t.ProvidesRoad());
				return buildRoad;
			}

			var best = (
				from at in accessibleTerraforms
				let aiScore = at.CalculateAIScore(player, t)
				where aiScore > 0
				orderby aiScore descending, at.TurnsToComplete
				select at
			).FirstOrDefault();

			return best;
		}

		private UnitAI.Result PerformWorkerMove(MapUnit unit, Terraform workerMove) {
			if (unit.canPerformTerraformAction(workerMove)) {
				unit.PerformTerraformAction(workerMove);
				return UnitAI.Result.InProgress;
			}

			return UnitAI.Result.Done;
		}

		// Given a list of tile candidates, return a plan to improve the nearest
		// unimproved tile.
		private static WorkerAIData? GetPlanToImproveNearestUnimproved(MapUnit unit, Player player, List<Tile> tiles) {
			List<Tile> nearestTiles =
				tiles.OrderBy(x => x.distanceTo(unit.location))
					.ThenByDescending(x => CityTileAssignmentAI.CalculateTileYieldScore(x, 2, player))
					.ToList();
			foreach (Tile t in nearestTiles) {
				Terraform? improvement = GetTileImprovement(t, unit);
				if (improvement != null) {
					WorkerAIData result = new () {
						workerMove = improvement,
						destination = t,
					};
					log.Information($"Set AI for unit at {unit.location} to {improvement} with destination of " + result.destination);

					PathingAlgorithm algorithm = PathingAlgorithmChooser.GetAlgorithm(unit);
					result.pathToDestination = algorithm.PathFrom(unit.location, result.destination, unit);

					return result;
				}
			}
			return null;
		}
	}
}
