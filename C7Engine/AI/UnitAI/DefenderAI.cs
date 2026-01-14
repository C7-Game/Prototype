using C7GameData;
using C7GameData.AIData;
using C7Engine.Pathing;
using System.Collections.Generic;
using System.Linq;
using Serilog;

namespace C7Engine.AI.UnitAI {
	class DefenderAI : C7GameData.UnitAI {
		private static ILogger log = Log.ForContext<DefenderAI>();
		public DefenderAIData data;

		public static DefenderAIData MakeAiDataForDefendInPlace(MapUnit unit, Player player) {
			DefenderAIData ai = new DefenderAIData();
			ai.goal = DefenderAIData.DefenderGoal.DEFEND_CITY;
			ai.destination = unit.location;
			log.Information("Set defender AI for " + unit + " with destination of " + ai.destination);
			return ai;
		}

		public static DefenderAIData? MakeAiDataForDefendAtRiskCity(MapUnit unit, Player player, int minDefenders) {
			if (!unit.CanDefendOnLand()) {
				// Just fortify in place if we can't defend on land.
				return MakeAiDataForDefendInPlace(unit, player);
			}

			City cityToDefend = FindAtRiskCityToDefend(unit, player, minDefenders);
			if (cityToDefend == null) {
				return null;
			}

			DefenderAIData ai = new DefenderAIData();
			ai.destination = cityToDefend.location;
			ai.goal = DefenderAIData.DefenderGoal.DEFEND_CITY;

			PathingAlgorithm algorithm = PathingAlgorithmChooser.GetAlgorithm(unit);
			ai.pathToDestination = algorithm.PathFrom(unit.location, ai.destination, unit);

			log.Information($"Unit {unit} tasked with defending {cityToDefend.name}");
			return ai;
		}

		public DefenderAI(DefenderAIData d) {
			data = d;
		}

		C7GameData.UnitAI.MoveResult C7GameData.UnitAI.PlayTurnImpl(Player player, MapUnit unit) {
			if (data.destination == unit.location) {
				if (!unit.isFortified) {
					unit.fortify();
					log.Information("Fortifying " + unit + " at " + data.destination);
				}
				return C7GameData.UnitAI.Result.Done;
			} else {
				log.Debug("Moving defender towards " + data.destination);
				return this.TryToMoveAlongPath(unit, ref data.pathToDestination, allowCombat: false);
			}
		}

		public string SummarizePlan() {
			return "DefenderAI: " + data.ToString();
		}

		public void UpdateOnDeath() { }

		/**
		 * Finds a nearby city that could use extra defenders.
		 *
		 * This is not a brilliant method, with many flaws such as whether the
		 * city needs more defenders, or if the units present are defenders.
		 */
		private static City FindAtRiskCityToDefend(MapUnit unit, Player player, int minDefenders) {
			if (player.cities.Count == 0) {
				return City.NONE;
			}

			// Assign a score to each city, where the highest score is the city
			// we want to send our unit to.
			Dictionary<City, float> cityScores = new();
			foreach (City c in player.cities) {
				int numDefenders = c.location.unitsOnTile.Count(u => u.CanDefendOnLand());
				float score = 0;
				if (numDefenders < minDefenders) {
					// Add to the score if there aren't many defenders, with a
					// larger score for less defended cities.
					score += 10f;
				}

				// Penalize cities for being far away.
				score -= c.location.distanceTo(unit.location);

				cityScores.Add(c, score);
			}

			// Make the city less important if there are already units on the
			// way.
			foreach (MapUnit u in player.units) {
				if (u.currentAI is DefenderAI defenderAi) {
					if (defenderAi.data.destination.cityAtTile != null) {
						cityScores[defenderAi.data.destination.cityAtTile] -= 3f;
					}
				}
			}

			float bestScore = (float)int.MinValue;
			City bestCity = null;
			foreach (KeyValuePair<City, float> p in cityScores) {
				if (p.Value > bestScore) {
					bestScore = p.Value;
					bestCity = p.Key;
				}
			}

			// If there are no cities in need of defending at this defense level,
			// don't return a city. This will let us decide to use units for
			// offense instead. We can always call this method with a larger
			// min defenders value if we really want to use defenders.
			if (bestScore <= 0 && minDefenders != int.MaxValue) {
				return null;
			}

			return bestCity;
		}
	}
}
