using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using C7GameData;
using System.Linq;
using C7GameData.AIData;
using Serilog;

namespace C7Engine {
	public class SettlerLocationAI {
		private static ILogger log = Log.ForContext<SettlerLocationAI>();

		//Eventually, there should be different weights based on whether the AI already
		//has the resource or not (more important to secure ones that they don't have).
		//But since we don't have trade networks yet, for now there's only one value.
		static int STRATEGIC_RESOURCE_BONUS = 20;
		static int LUXURY_RESOURCE_BONUS = 15;

		//Figures out where to plant Settlers
		public static Tile findSettlerLocation(Tile start, Player player) {
			Dictionary<Tile, int> scores = GetScoredSettlerCandidates(start, player);
			if (scores.Count == 0 || scores.Values.Max() <= 0) {
				return Tile.NONE;   //nowhere to settle
			}

			IOrderedEnumerable<KeyValuePair<Tile, int> > orderedScores = scores.OrderByDescending(t => t.Value);
			log.Debug("Top city location candidates from " + start + ":");
			Tile returnValue = null;
			foreach (KeyValuePair<Tile, int> kvp in orderedScores.Take(5)) {
				if (returnValue == null) {
					returnValue = kvp.Key;
				}
				if (kvp.Value > 0) {
					log.Debug("  Tile " + kvp.Key + " scored " + kvp.Value);
				}
			}
			return returnValue;
		}

		public static Dictionary<Tile, int> GetScoredSettlerCandidates(Tile start, Player player) {
			List<MapUnit> playerUnits = player.units;
			IEnumerable<Tile> candidates = player.tileKnowledge.AllKnownTiles().Where(t => !IsInvalidCityLocation(t));
			Dictionary<Tile, int> scores = AssignTileScores(start, player, candidates, playerUnits.FindAll(u => u.unitType.name == "Settler"));
			return scores;
		}

		private static Dictionary<Tile, int> AssignTileScores(Tile startTile, Player player, IEnumerable<Tile> candidates, List<MapUnit> playerSettlers) {
			Dictionary<Tile, int> scores = new Dictionary<Tile, int>();
			candidates = candidates.Where(t => !SettlerAlreadyMovingTowardsTile(t, playerSettlers) && t.IsAllowCities());
			foreach (Tile t in candidates) {
				int score = GetTileYieldScore(t, player);
				//For simplicity's sake, I'm only going to look at immediate neighbors here, but
				//a lot more things should be considered over time.
				foreach (Tile nt in t.neighbors.Values) {
					score += GetTileYieldScore(nt, player);
				}
				//TODO: Also look at the next ring out, with lower weights.

				//Prefer hills for defense, and coast for boats and such.
				if (t.baseTerrainType.Key == "hills") {
					score += 10;
				}
				if (t.NeighborsWater()) {
					score += 10;
				}

				//Lower scores if they are far away
				int preDistanceScore = score;
				int distance = startTile.distanceTo(t);
				if (distance > 4) {
					score -= distance * 2;
				}
				//Distance can never lower score beyond 1; the AI will always try to settle those worthless tundras.
				//(This could actually be modified in the future, but for now is also a safety rail)
				if (preDistanceScore > 0 && score <= 0) {
					score = 1;
				}
				if (score > 0)
					scores[t] = score;
			}
			return scores;
		}
		private static int GetTileYieldScore(Tile t, Player owner) {
			int score = t.foodYield(owner) * 5;
			score += t.productionYield(owner) * 3;
			score += t.commerceYield(owner) * 2;
			if (t.Resource.Category == ResourceCategory.STRATEGIC) {
				score += STRATEGIC_RESOURCE_BONUS;
			} else if (t.Resource.Category == ResourceCategory.LUXURY) {
				score += LUXURY_RESOURCE_BONUS;
			}
			return score;
		}

		public static bool IsInvalidCityLocation(Tile tile) {
			if (tile.HasCity) {
				log.Verbose("Tile " + tile + " is invalid due to existing city of " + tile.cityAtTile.name);
				return true;
			}
			foreach (Tile neighbor in tile.neighbors.Values) {
				if (neighbor.HasCity) {
					log.Verbose("Tile " + tile + " is invalid due to neighboring city of " + neighbor.cityAtTile.name);
					return true;
				}
				foreach (Tile neighborOfNeighbor in neighbor.neighbors.Values) {
					if (neighborOfNeighbor.HasCity) {
						log.Verbose("Tile " + tile + " is invalid due to nearby city of " + neighborOfNeighbor.cityAtTile.name);
						return true;
					}
				}
			}

			log.Debug("Tile " + tile + " is a valid city location ");
			return false;
		}

		/// <summary>
		/// Returns true if one of the settlers in the list (which should be the list of the current AI's settlers) is
		/// already heading to a tile near the requested tile.
		/// Does not return true if only another AI's settlers are headed there, as the AI shouldn't know the other
		/// AI's plans.
		/// </summary>
		/// <param name="tile">The tile under consideration for a future city.</param>
		/// <param name="playerSettlers">The settlers owned by the AI considering building a city.</param>
		/// <returns></returns>
		public static bool SettlerAlreadyMovingTowardsTile(Tile tile, List<MapUnit> playerSettlers) {
			foreach (MapUnit otherSettler in playerSettlers) {
				if (otherSettler.currentAIData is SettlerAIData otherSettlerAI) {
					if (otherSettlerAI.destination == tile) {
						return true;
					}
					if (otherSettlerAI.destination.GetLandNeighbors().Exists(innerRingTile => innerRingTile == tile)) {
						return true;
					}
					foreach (Tile innerRingTile in otherSettlerAI.destination.GetLandNeighbors()) {
						if (innerRingTile.GetLandNeighbors().Exists(outerRingTile => outerRingTile == tile)) {
							return true;
						}
					}
				}
			}
			return false;
		}
	}
}
