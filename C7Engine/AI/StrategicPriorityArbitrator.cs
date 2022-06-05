using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using C7Engine.AI.StrategicAI;
using C7GameData;
using C7GameData.AIData;

namespace C7Engine.AI {

	/**
	 * Evaluates possible strategic priorities for the AI, and decides upon which ones the AI will pursue.
	 */
	public class StrategicPriorityArbitrator {

		/// <summary>
		/// N.B. I want to have this support primary/secondary/tertiary priorities.  That will require more params, so
		/// it doesn't choose the same priority as primary/secondary/tertiary.
		///
		/// However, right now there's enough complexity that I want to stop and check that it's working for one.  I will
		/// improve it in a future commit.
		/// </summary>
		/// <param name="player">The player whose priorities are being arbitrated.</param>
		/// <returns>The chosen priority.</returns>
		public static StrategicPriority Arbitrate(Player player) {
			List<Type> priorityTypes = PriorityAggregator.GetAllStrategicPriorityTypes();
			List<StrategicPriority> possiblePriorities = new List<StrategicPriority>();

			foreach (Type priorityType in priorityTypes) {
				ConstructorInfo constructor = priorityType.GetConstructor(Type.EmptyTypes);
				if (constructor != null) {
					object instance = constructor.Invoke(Array.Empty<object>());
					StrategicPriority priority = (StrategicPriority)instance;
					priority.CalculateWeightAndMetadata(player);
					possiblePriorities.Add(priority);
				} else {
					Console.WriteLine($"Zero-argument constructor for priority {priorityType} not found; skipping that priority type");
				}
			}

			return ChooseStrategicPriority(possiblePriorities, PrioritizationType.WEIGHTED_LINEAR);
		}
		private static StrategicPriority ChooseStrategicPriority(List<StrategicPriority> possiblePriorities, PrioritizationType weighting) {
			if (weighting == PrioritizationType.ALWAYS_CHOOSE_HIGHEST_SCORE) {
				return FindTopScoringPriority(possiblePriorities);
			} else {
				return WeightedPriority(possiblePriorities, weighting);
			}
		}

		private static StrategicPriority FindTopScoringPriority(List<StrategicPriority> possiblePriorities) {
			float max = possiblePriorities[0].GetCalculatedWeight();
			StrategicPriority topScore = possiblePriorities[0];
			foreach (StrategicPriority priority in possiblePriorities) {
				if (priority.GetCalculatedWeight() > max) {
					max = priority.GetCalculatedWeight();
					topScore = priority;
				}
			}
			Console.WriteLine($"Chose priority {topScore} with score {max}");
			return topScore;
		}

		private static StrategicPriority WeightedPriority(List<StrategicPriority> possiblePriorities, PrioritizationType weighting) {
			double sumOfAllWeights = 0.0;
			List<double> cutoffs = new List<double>();
			foreach (StrategicPriority possiblePriority in possiblePriorities) {
				double baseWeight = possiblePriority.GetCalculatedWeight();
				double adjustedWeight = AdjustWeightByFactor(baseWeight, weighting);

				double oldCutoff = sumOfAllWeights;
				sumOfAllWeights += adjustedWeight;

				Console.WriteLine($"Priority {possiblePriority} has range of {oldCutoff} to {sumOfAllWeights}");

				cutoffs.Add(sumOfAllWeights);
			}

			Random random = new Random();
			double randomDouble = sumOfAllWeights * random.NextDouble();
			Console.WriteLine($"Random number in range 0 to {sumOfAllWeights} is {randomDouble}");
			int idx = 0;
			foreach (double cutoff in cutoffs) {
				if (randomDouble < cutoff) {
					Console.WriteLine($"Chose priority {possiblePriorities[idx]}");
					return possiblePriorities[idx];
				}
				idx++;
			}
			return new WarPriority();	//TODO: Fallback
		}

		private static double AdjustWeightByFactor(double baseWeight, PrioritizationType weighting) {
			if (weighting == PrioritizationType.WEIGHTED_QUADRATIC) {
				return baseWeight * baseWeight;
			} else {
				return baseWeight;
			}
		}
	}
}
