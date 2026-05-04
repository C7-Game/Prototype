using System;
using System.Collections.Generic;
using System.Linq;
using Serilog;
using C7GameData;
using static C7GameData.PlayerRelationship;

namespace C7Engine {
	public class ChooseProducible {
		private static ILogger log = Log.ForContext<ChooseProducible>();

		record struct ProducibleStats(
			float bestAttack,
			float bestDefense,
			float bestNonWonderCulture,
			int numberOfReachableOpenCitySpots,
			bool inExpansionPhase
		);

		public static IProducible Choose(City city, Player player) {
			List<IProducible> options = city.ListProductionOptions(EngineStorage.gameData).ToList();
			ProducibleStats stats = CalculateStats(city, options);

			IProducible best = null;
			float bestScore = int.MinValue;

			log.Debug($"{player.civilization.name}: {city}---- {stats.inExpansionPhase} {stats.numberOfReachableOpenCitySpots}");
			foreach (IProducible option in options) {
				// Get the item score, with a +/- 10% random adjustment to make
				// things seem appropriately random.
				float score = ScoreProducible(stats, city, player, option) * (GameData.rng.Next(90, 110) / 100.0f);
				if (score > bestScore) {
					bestScore = score;
					best = option;
				}
				log.Debug($"\t{option}: {score}");
			}
			log.Debug($"\t\tchose {best}");
			return best;
		}

		private static float ScoreProducible(ProducibleStats stats, City city, Player player, IProducible option) {
			if (option is UnitPrototype unit) {
				return ScoreUnit(stats, city, player, unit);
			} else if (option is Building building) {
				return ScoreBuilding(stats, city, player, building);
			} else if (option is Inflow inflow) {
				return ScoreInflow(stats, city, player, inflow);
			} else {
				throw new Exception($"Unexpected producible: {option}");
			}
		}

		private static float ScoreInflow(ProducibleStats stats, City city, Player player, Inflow inflow) {
			// TODO: score this properly, right now we give a very low score to avoid auto picking this
			// I am not sure how civ III does this, although I guess we should consider stuff like
			// no more buildings to build, reached unit cap, very bad economy with no other options,
			// the WealthNever and WealthOften flags from RACE, etc
			return -1000f;
		}

		private static float ScoreUnit(ProducibleStats stats, City city, Player player, UnitPrototype unit) {
			bool isSettler = unit.actions.Contains(UnitAction.BuildCity);
			bool isWorker = unit.isWorker;
			bool atWar = IsInAnyWar(player, EngineStorage.gameData.players);
			bool cityGuarded = city.location.unitsOnTile.Count(u => u.CanDefendOnLand()) > 0;
			bool hasUnescortedSettler = HasUnescortedSettler(city);
			var (totalUnits, allowedUnits, unitSupportCost) = player.TotalUnitsAllowedUnitsAndSupportCost();

			float attackWeight = atWar ? 15 : 10;
			float defenseWeight = atWar ? 15 : 10;
			float populationWeight = 10;
			float populationCostPenalty = populationWeight * unit.populationCost;
			float noTradeAccessBoost = 10;
			float unitSupportCapPenalty = 10;

			float score = 0;

			// Create a fake version of this unit so we can check what AI we
			// would give it.
			MapUnit temp = unit.GetInstance(EngineStorage.gameData.GenerateID(unit.name), unit, player, location: city.location);

			////////////////////////////////////////////////////////////////////
			///
			/// Raw score adjustments based on unit stats.
			///

			// Weight the unit's attack and defense score by comparing it to the
			// best stat available.
			score += unit.attack / stats.bestAttack * attackWeight;
			score += unit.defense / stats.bestDefense * defenseWeight;

			// Penalize more expensive units.
			score -= city.TurnsToProduce(unit);

			// Penalize units that consume population.
			score -= populationCostPenalty;

			////////////////////////////////////////////////////////////////////
			///
			/// Naval units
			///

			if (unit.categories.Contains("Sea")) {
				// Don't bother building naval units unless we border the ocean.
				// This does mean that inland seas are excluded.
				if (!city.location.NeighborsOcean()) {
					return int.MinValue;
				}

				// Don't built a naval unit if we wouldn't explore with it. We
				// don't yet handle using ships as transports or naval warfare.
				if (!(PlayerAI.GetAIForUnit(temp, player) is ExplorerAI)) {
					return int.MinValue;
				}
			}

			////////////////////////////////////////////////////////////////////
			///
			/// Adjustments based on the city situation.
			///

			// Prioritize defending the city if it is unguarded.
			if (!cityGuarded && unit.defense == 0) {
				return int.MinValue;
			}

			// Prioritize building settler escorts if we don't have one.
			if (hasUnescortedSettler && unit.defense == 0) {
				return int.MinValue;
			}

			// Penalize going over the unit support cap unless we're at war or
			// still in the expansion phase.
			if (unitSupportCost > 0 && !atWar && !stats.inExpansionPhase) {
				score -= unitSupportCost / 2;
			}

			////////////////////////////////////////////////////////////////////
			///
			/// Adjustments for settlers and workers.
			///

			// Don't built a worker or settler if we don't have enough population.
			if (CityIsTooSmall(city, unit)) {
				return int.MinValue;
			}

			if (isSettler) {
				// Don't build settlers if we don't have anywhere to go or if we
				// already have enough settlers under construction to fill all
				// the spots.
				if (stats.numberOfReachableOpenCitySpots <= SettlersUnderConstruction(player)) {
					return int.MinValue;
				}

				// If there are more open spots than we have cities we are still
				// aggressively expanding. Negate the population penalty, and
				// weight settlers more heavily if there are way more open spots.
				if (stats.numberOfReachableOpenCitySpots > player.cities.Count) {
					score += populationCostPenalty * Math.Min(3.0f, stats.numberOfReachableOpenCitySpots / player.cities.Count);
				}

				// Slightly penalize going over the optimal number of cities.
				if (player.cities.Count > EngineStorage.gameData.map.optimalNumberOfCities) {
					score *= 2.0f / 3.0f;
				}
			}

			if (isWorker) {
				// If we have unworked tiles and fewer workers than cities, boost
				// the odds of producing a worker.
				int numUnworkedTiles = NumUnworkedTiles(city);
				int numWorkers = player.units.Count(x => x.unitType.isWorker);
				if (numUnworkedTiles > 0 && numWorkers < player.cities.Count * 1.5f) {
					score += populationCostPenalty * Math.Min(3.0f, 1 + numUnworkedTiles);
				}

				// If we have a few cities but don't have trade access to the
				// capital, boost the odds of a worker.
				if (!EngineStorage.gameData.GetTradeNetwork().ConnectedToCapital(player, city) && player.cities.Count > 4) {
					score += noTradeAccessBoost;
				}
			}

			return score;
		}

		private static float ScoreBuilding(ProducibleStats stats, City city, Player player, Building building) {
			bool atWar = IsInAnyWar(player, EngineStorage.gameData.players);

			float score = 0;

			// Marketplaces should be prioritized in cities with high population
			// that have a decent number of luxuries. Otherwise they don't do
			// much.
			if (building.increasesLuxuryTrade) {
				GameData gd = EngineStorage.gameData;
				score += Math.Max(city.residents.Count - city.GetLuxuries(gd).Keys.Count, 0) * (city.GetLuxuries(gd).Keys.Count / 2);
			}

			// Only build an aqueduct if we need one.
			if (building.allowsCitySize2 &&
				city.residents.Count == player.rules.MaximumLevel1CitySize && city.FoodGrowthPerTurn() > 0) {
				score += 50;
			}

			// Ditto with hospitals.
			if (building.allowsCitySize3 &&
				city.residents.Count == player.rules.MaximumLevel2CitySize && city.FoodGrowthPerTurn() > 0) {
				score += 70;
			}

			// A town that is at war might consider building walls, otherwise it
			// probably shouldn't.
			if (building.providesWalls) {
				score += atWar ? 20 : 8;
			}

			// If a city has a decent number of corrupt shields, prioritize
			// building a courthouse based on how many corrupt shields are in
			// play.
			//
			// Ensure we don't build a courthouse in the capital though!
			if (building.reducesCorruption) {
				if (city.capital) {
					return int.MinValue;
				}

				CorruptableValue prod = city.CurrentProductionYield();
				if (prod.useful > 0 && ((float)prod.corrupt) / prod.useful > .15) {
					score += prod.corrupt * 8;
				}
			}

			// Boost buildings that help with happiness if we have some unhappy
			// citizens in the city or if we have a decently large value on the
			// luxury slider.
			if (building.contentFacesInCity > 0) {
				int unhappyCount = 0;
				int entertainerCount = 0;
				foreach (CityResident cr in city.residents) {
					if (cr.mood == CityResident.Mood.Unhappy) { ++unhappyCount; }
					if (cr.citizenType.Luxuries > 0) { ++entertainerCount; }
				}

				if (unhappyCount > 0 || (player.luxuryRate > 3 && city.residents.Count > 4)) {
					score += Math.Min(unhappyCount, building.contentFacesInCity) * 10;
					score += Math.Min(entertainerCount, building.contentFacesInCity) * 10;
				}
			}

			// Boost buildings that provide culture, especially if could claim
			// additional tiles by expanding our borders, or if we are next to
			// an enemy city.
			if (building.culturePerTurn > 0) {
				int unclaimedTilesInOuterRing = 0;
				int enemyTilesInOuterRing = 0;
				foreach (Tile t in city.location.GetTilesWithinRankDistance(2)) {
					if (t.OwningPlayer() == null) {
						++unclaimedTilesInOuterRing;
					} else if (t.OwningPlayer() != city.owner) {
						++enemyTilesInOuterRing;
					}
				}

				if (city.GetCulturePerTurn() == 0) {
					score += building.culturePerTurn * (unclaimedTilesInOuterRing + enemyTilesInOuterRing * 3);
				} else {
					score += building.culturePerTurn * (unclaimedTilesInOuterRing + enemyTilesInOuterRing * 3) / 2.0f;
				}
			}

			// Prioritize barracks in larger cities, and especially if we are at
			// war.
			if (building.providesVeteranGroundUnits) {
				score += (city.residents.Count / 5) * (atWar ? 3 : 1);
			}

			// Penalize more expensive buildings.
			score -= city.TurnsToProduce(building) / (stats.inExpansionPhase ? 1 : 2);

			// Penalize buildings with higher maintenance costs.
			score -= building.maintenanceCost;

			// Boost buildings a bit if we aren't at war and aren't expanding.
			if (!stats.inExpansionPhase && !atWar) {
				score += 20;
			}

			return score;
		}

		private static ProducibleStats CalculateStats(City city, List<IProducible> options) {
			ProducibleStats stats = new() {
				numberOfReachableOpenCitySpots = NumberOfReachableOpenCitySpots(city),
			};
			stats.inExpansionPhase = city.owner.cities.Count < 5 || stats.numberOfReachableOpenCitySpots > city.owner.cities.Count * 2;

			foreach (IProducible option in options) {
				if (option is UnitPrototype unit) {
					stats.bestAttack = Math.Max(stats.bestAttack, unit.attack);
					stats.bestDefense = Math.Max(stats.bestDefense, unit.defense);
				} else if (option is Building building && building.greatWonderProperties != null) {
					stats.bestNonWonderCulture = Math.Max(stats.bestNonWonderCulture, building.culturePerTurn);
				}
			}

			return stats;
		}

		private static bool HasUnescortedSettler(City city) {
			foreach (MapUnit u in city.location.unitsOnTile) {
				if (u.currentAI is SettlerAI settlerAi && settlerAi.data.escort == null) {
					return true;
				}
			}
			return false;
		}

		private static int NumberOfReachableOpenCitySpots(City city) {
			int result = 0;

			// Note: GetScoredSettlerCandidates already excludes tiles with
			// settlers moving towards them.
			Dictionary<Tile, float> scoredLocations = SettlerLocationAI.GetScoredSettlerCandidates(city.location, city.owner);
			List<KeyValuePair<Tile, float>> orderedScores = scoredLocations.OrderByDescending(t => t.Value).ToList();

			foreach ((Tile tile, float score) in orderedScores) {
				if (scoredLocations.ContainsKey(tile)) {
					++result;

					// Remove all the spots that would become invalid locations
					// if we built this city.
					foreach (Tile n in tile.neighbors.Values) {
						scoredLocations.Remove(n);
					}
				}
			}

			return result;
		}

		private static int SettlersUnderConstruction(Player player) {
			int result = 0;
			foreach (City c in player.cities) {
				if (c.itemBeingProduced is UnitPrototype unit && unit.actions.Contains(UnitAction.BuildCity)) {
					++result;
				}
			}
			return result;
		}

		private static bool CityIsTooSmall(City city, UnitPrototype unit) {
			if (unit.populationCost < city.residents.Count) {
				return false;
			}

			// If we would grow before the city finishes producing the unit, we
			// can build the unit.
			if (unit.populationCost == city.residents.Count && city.TurnsToProduce(unit) >= city.TurnsUntilGrowth()) {
				return false;
			}

			return true;
		}

		private static int NumUnworkedTiles(City city) {
			int result = 0;
			foreach (CityResident cr in city.residents) {
				if (cr.tileWorked != Tile.NONE && !cr.tileWorked.overlays.HasBeenImproved()) {
					++result;
				}
			}
			return result;
		}
	}
}
