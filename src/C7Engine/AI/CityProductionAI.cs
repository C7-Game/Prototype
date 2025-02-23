using System;
using System.Collections.Generic;
using System.Linq;
using C7Engine.AI.StrategicAI;
using Serilog;

namespace C7Engine {
	using C7GameData;

	/**
	 * A simple AI for choosing what to produce next.
	 * We probably will have a few variants of this, an interface, etc.
	 * eventually.  For now, I just want to separate it out from the main
	 * interaction events and make it clear that it's an AI component.
	 */
	public class CityProductionAI {

		private static ILogger log = Log.ForContext<CityProductionAI>();

		/**
		 * Gets the next item to be produced in a given city.
		 * Not a final API; it probably has the wrong parameters.  The last item in this city shouldn't
		 * matter, unless the "always build previously build unit" option is enabled, in which case it isn't necessary
		 * to call this method, just build the same thing.
		 *
		 * But what are the right parameters?  That's a tougher question.  We might want to consider a bunch of things.
		 * If there's a war going on.  What victory condition we're going for.  If we're broke and need more marketplaces.
		 * Maybe we'll wind up with some sort of collection of AI parameters to pass someday?  For now I'm not going to
		 * get hung up on knowing exactly how it should be done the road.
		 */
		public static IProducible GetNextItemToBeProduced(City city, IProducible lastProduced) {
			List<StrategicPriority> priorities = city.owner.strategicPriorityData;
			IEnumerable<IProducible> unitPrototypes = city.ListProductionOptions();

			log.Information($"Choosing what to produce next in {city.name}");

			List<IProducible> prototypes = new List<IProducible>();
			List<float> weights = new List<float>();

			//N.B. This implicitly casts to UnitPrototype.  For now this is fine but once we add buildings, this (or the source of the list)
			//will have to get smarter.
			foreach (UnitPrototype unitPrototype in unitPrototypes) {
				float baseScore = GetItemScore(unitPrototype);
				log.Debug($" Base score for {unitPrototype} is {baseScore}");
				// There may eventually be some additive adjusters (or that may play into the previous)
				float flatAdjuster = GetPriorityFlatAdjusters(priorities, unitPrototype, baseScore);
				float flatAdjustedScore = baseScore + flatAdjuster;
				log.Debug($"  Flat-adjusted score for {unitPrototype} is {flatAdjustedScore}");


				//Exclude naval units from land-only cities
				if (unitPrototype.categories.Contains("Sea") && !city.location.NeighborsWater()) {
					flatAdjustedScore = 0.0f;
				}

				// Below here are multiplicative adjusters
				float popAdjustedScore = AdjustScoreByPopCost(city, unitPrototype, flatAdjustedScore);
				log.Debug($" {unitPrototype.name} pop-adjusted-scores {popAdjustedScore}");
				float priorityAdjustedScore = AdjustScoreByPriorities(priorities, unitPrototype, popAdjustedScore);
				log.Debug($" {unitPrototype.name} priority-adjusted-scores {priorityAdjustedScore}");
				prototypes.Add(unitPrototype);
				weights.Add(priorityAdjustedScore);
			}

			IProducible chosen = ChooseWeightedPriority(prototypes, weights, Weighting.WEIGHTED_QUADRATIC);
			return chosen;
		}

		/// <summary>
		/// Gets a "base score" for a unit.
		/// This is our first primitive comparison utility.
		/// Eventually it should be influenced by a lot of different factors.
		/// </summary>
		/// <param name="item"></param>
		/// <returns></returns>
		public static float GetItemScore(IProducible item) {
			float baseScore = 0.0f;
			if (item is UnitPrototype unit) {
				baseScore = baseScore + 10 * unit.attack;
				baseScore = baseScore + 10 * unit.defense;
				if (unit.movement > 1) {
					//Multiple by 0.5 for each movement point
					//N.B. Eventually this should be influenced by the military strategy
					baseScore = baseScore + baseScore / 2 * (unit.movement - 1);
				}
				baseScore = baseScore - unit.shieldCost;
				baseScore = baseScore - 10 * unit.populationCost;
				baseScore = baseScore + 5 * unit.bombard;
				return baseScore;
			}
			return 0.0f;
		}

		private static IProducible ChooseWeightedPriority(List<IProducible> items, List<float> weights, Weighting weighting) {
			double sumOfAllWeights = 0.0;
			List<double> cutoffs = new List<double>();
			int i = 0;
			foreach (float f in weights) {
				double baseWeight = f;
				double adjustedWeight = WeightAdjuster.AdjustWeightByFactor(baseWeight, weighting);
				if (f <= 0) {
					adjustedWeight = 0;
				}

				double oldCutoff = sumOfAllWeights;
				sumOfAllWeights += adjustedWeight;

				log.Verbose($"Item {items[i]} has range of {oldCutoff} to {sumOfAllWeights}");

				cutoffs.Add(sumOfAllWeights);
				i++;
			}

			double randomDouble = sumOfAllWeights * GameData.rng.NextDouble();
			log.Verbose($"Random number in range 0 to {sumOfAllWeights} is {randomDouble}");
			int idx = 0;
			foreach (double cutoff in cutoffs) {
				if (randomDouble < cutoff) {
					log.Information($"Chose item {items[idx]}");
					return items[idx];
				}
				idx++;
			}
			return items[0];    //TODO: Fallback
		}

		public static float AdjustScoreByPopCost(City city, UnitPrototype unitPrototype, float baseScore) {
			//If the city isn't going to grow in time, return 0
			if (unitPrototype.populationCost > 0) {
				int size = city.size;
				//Don't allow starting on something that costs more pop than we have
				if (unitPrototype.populationCost > size) {
					return 0.0f;
				}
				//If it costs our whole pop, check whether we'll grow in time
				if (unitPrototype.populationCost == size) {
					int turnsTillGrowth = city.TurnsUntilGrowth();
					int turnsToBuild = city.TurnsToProduce(unitPrototype);
					if (turnsTillGrowth > turnsToBuild) {
						return 0.0f;
					} else {
						//We'll grow in time, but be back to size 1
						//Apply a penalty
						return baseScore / 2;
					}
				}
				//If we get here, it has a pop cost, but we already have enough pop to build it
				//Pop cost is already factored in as an additive factor, so we won't adjust it
				//Eventually, this should be enhanced by various other considerations, and be moddable.
			}
			return baseScore;
		}

		public static float GetPriorityFlatAdjusters(List<StrategicPriority> priorities, UnitPrototype prototype, float baseScore) {
			// How much emphasis the top priority adds.
			float totalAdjusters = 0.0f;
			float priorityMultiplier = 1.0f;
			foreach (StrategicPriority priority in priorities) {
				float adjuster = priority.GetProductionItemFlatAdjuster(prototype);
				// Low-level log, we don't have proper logs yet so it's commented out
				log.Verbose($"  Got adjuster of {adjuster} from {priority}; adjusting by ${adjuster * priorityMultiplier}");
				totalAdjusters += (adjuster * priorityMultiplier);
				priorityMultiplier /= 2;
			}
			return totalAdjusters;
		}

		public static float AdjustScoreByPriorities(List<StrategicPriority> priorities, UnitPrototype prototype, float baseScore) {
			// How much emphasis the top priority adds.
			float priorityMultiplier = 1.0f;
			foreach (StrategicPriority priority in priorities) {
				float adjuster = priority.GetProductionItemPreferenceWeight(prototype);
				baseScore = baseScore + (adjuster * priorityMultiplier) * baseScore;
				priorityMultiplier /= 2;
			}
			return baseScore;
		}
	}
}
