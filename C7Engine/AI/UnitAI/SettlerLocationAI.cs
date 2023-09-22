using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using C7GameData;
using System.Linq;
using C7GameData.AIData;
using Serilog;

namespace C7Engine
{
	public class SettlerLocationAI {
		private static ILogger log = Log.ForContext<SettlerLocationAI>();

		//Eventually, there should be different weights based on whether the AI already
		//has the resource or not (more important to secure ones that they don't have).
		//But since we don't have trade networks yet, for now there's only one value.
		static int STRATEGIC_RESOURCE_BONUS = 20;
		static int LUXURY_RESOURCE_BONUS = 15;

		//Figures out where to plant Settlers
		public static Tile findSettlerLocation(Tile start, Player player) {
			Dictionary<Tile, double> scores = GetScoredSettlerCandidates(start, player);
			if (scores.Count == 0 || scores.Values.Max() <= 0) {
				return Tile.NONE;	//nowhere to settle
			}

			IOrderedEnumerable<KeyValuePair<Tile, double> > orderedScores = scores.OrderByDescending(t => t.Value);
			log.Debug("Top city location candidates from " + start + ":");
			Tile returnValue = null;
			foreach (KeyValuePair<Tile, double> kvp in orderedScores.Take(5))
			{
				if (returnValue == null) {
					returnValue = kvp.Key;
				}
				if (kvp.Value > 0) {
					log.Debug("  Tile " + kvp.Key + " scored " + kvp.Value);
				}
			}
			return returnValue;
		}

		public static Dictionary<Tile, double> GetScoredSettlerCandidates(Tile start, Player player) {
			List<MapUnit> playerUnits = player.units;
			IEnumerable<Tile> candidates = player.tileKnowledge.AllKnownTiles().Where(t => !IsInvalidCityLocation(t));
			Dictionary<Tile, double> scores = AssignTileScores(start, player, candidates, playerUnits.FindAll(u => u.unitType.name == "Settler"));
			return scores;
		}

		private static Dictionary<Tile, double> AssignTileScores(Tile startTile, Player player, IEnumerable<Tile> candidates, List<MapUnit> playerSettlers)
		{
			Dictionary<Tile, double> scores = new Dictionary<Tile, double>();
			candidates = candidates.Where(t => !SettlerAlreadyMovingTowardsTile(t, playerSettlers) && t.IsAllowCities());
			foreach (Tile t in candidates) {
				double score = GetTileYieldScore(t, player);
				//For simplicity's sake, I'm only going to look at immediate neighbors here, but
				//a lot more things should be considered over time.
				foreach (Tile nt in t.neighbors.Values) {
					double improvementScore = GetTileImprovementScore(nt, player);
					double yieldScore = GetTileYieldScore(nt, player);
					log.Information("Neighbor tile has score of " + yieldScore);
					log.Information("Neighbor tile has improvement score of " + improvementScore);
					score += yieldScore;
				}
				//Also look at the next ring out, with lower weights.
				foreach (Tile outerTile in t.neighbors.Values)
				{
					double outerTileScore = (GetTileYieldScore(outerTile, player) + GetTileImprovementScore(outerTile, player)) / 3;
					score += outerTileScore;
					log.Information("Outer ring tile has yield score of " + outerTileScore);
				}

				//Prefer hills for defense, and coast for boats and such.
				// In this scale, the defense bonus of hills adds up to a bonus of +10, which is equivalent to the previous hardcoded bonus. This just opens up possibilities with editing terrain.
				score += t.baseTerrainType.defenseBonus.amount * 20.0;
				
				// Need to add a way to check freshwater source, and separately to check if coast is lake or coast tile. This score would not apply if the city only borders a lake, although we still need a freshwater bonus

				//Lower scores if they are far away
				double preDistanceScore = score;
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
				log.Information("Tile score for settling is " + score);
			}
			return scores;
		}
		private static double GetTileYieldScore(Tile t, Player owner)
		{
			double score = t.foodYield(owner) * 5;
			score += t.productionYield(owner) * 3;
			score += t.commerceYield(owner) * 2;

			// TODO: Add multipliers for resource rarity, utility, and whether this player has a surplus
			if (t.Resource.Category == ResourceCategory.STRATEGIC) {
				score += STRATEGIC_RESOURCE_BONUS;
			}
			else if (t.Resource.Category == ResourceCategory.LUXURY) {
				score += LUXURY_RESOURCE_BONUS;
			}
			return score;
		}

		private static double GetTileImprovementScore (Tile t, Player owner)
		{
			double irrigationBonus = t.irrigationYield(owner);
			double mineBonus = t.miningYield();

			// Food is more important than production 
			double irrigationValue = irrigationBonus * 5;
			double mineValue = mineBonus * 3;

			// Since we can only irrigate OR mine, we just return the max of the two
			return Math.Max(irrigationValue,mineValue);
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
			foreach (MapUnit otherSettler in playerSettlers)
			{
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
