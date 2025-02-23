using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using C7Engine.AI.StrategicAI;
using C7GameData;
using C7GameData.AIData;
using Serilog;

namespace C7Engine.AI {

	/**
	 * Evaluates possible strategic priorities for the AI, and decides upon which ones the AI will pursue.
	 */
	public class StrategicPriorityArbitrator {

		private static ILogger log = Log.ForContext<StrategicPriorityArbitrator>();

		private static readonly int MIN_PRIORITIES = 2;
		private static readonly int MAX_PRIORITIES = 4;

		/// <summary>
		/// Returns an ordered list of AI priorities, with the first item being the top priority.
		/// </summary>
		/// <param name="player">The player whose priorities are being arbitrated.</param>
		/// <returns>The chosen priorities, in order.</returns>
		public static List<StrategicPriority> Arbitrate(Player player) {
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
					log.Warning($"Zero-argument constructor for priority {priorityType} not found; skipping that priority type");
				}
			}

			int numberOfPriorities = CalculateNumberOfPriorities(possiblePriorities);

			List<StrategicPriority> priorities = new List<StrategicPriority>();
			for (int i = 0; i < numberOfPriorities; i++) {
				StrategicPriority topPriority = ChooseStrategicPriority(possiblePriorities, Weighting.WEIGHTED_LINEAR);
				priorities.Add(topPriority);
				possiblePriorities.Remove(topPriority);
			}
			return priorities;
		}

		/// <summary>
		/// Figures out how many priorities the AI will choose.
		/// The idea is that if there are a bunch of things all about equally important, it should try to balance between
		/// them a bit.  If there's one overwhelming priority, it should disregard less important concerns.
		/// </summary>
		/// <param name="possiblePriorities"></param>
		/// <returns></returns>
		private static int CalculateNumberOfPriorities(List<StrategicPriority> possiblePriorities) {
			int count = 1;
			possiblePriorities.Sort((a, b) => {
				return a.GetCalculatedWeight() - b.GetCalculatedWeight() > 0 ? 1 : -1;
			});
			float previousWeight = possiblePriorities[0].GetCalculatedWeight();
			for (int idx = 1; idx < possiblePriorities.Count; idx++) {
				float nextWeight = possiblePriorities[idx].GetCalculatedWeight();
				if (nextWeight < previousWeight / 3) {
					break;
				}
				count++;
				previousWeight = nextWeight;
			}
			if (count < MIN_PRIORITIES) {
				return MIN_PRIORITIES;
			}
			if (count > MAX_PRIORITIES) {
				return MAX_PRIORITIES;
			}
			return count;
		}
		private static StrategicPriority ChooseStrategicPriority(List<StrategicPriority> possiblePriorities, Weighting weighting) {
			if (weighting == Weighting.ALWAYS_CHOOSE_HIGHEST_SCORE) {
				return FindTopScoringPriority(possiblePriorities);
			} else {
				return ChooseWeightedPriority(possiblePriorities, weighting);
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
			log.Information($"Chose priority {topScore} with score {max}");
			return topScore;
		}

		private static StrategicPriority ChooseWeightedPriority(List<StrategicPriority> possiblePriorities, Weighting weighting) {
			double sumOfAllWeights = 0.0;
			List<double> cutoffs = new List<double>();
			foreach (StrategicPriority possiblePriority in possiblePriorities) {
				double baseWeight = possiblePriority.GetCalculatedWeight();
				double adjustedWeight = WeightAdjuster.AdjustWeightByFactor(baseWeight, weighting);

				double oldCutoff = sumOfAllWeights;
				sumOfAllWeights += adjustedWeight;

				log.Debug($"Priority {possiblePriority} has range of {oldCutoff} to {sumOfAllWeights}");

				cutoffs.Add(sumOfAllWeights);
			}

			double randomDouble = sumOfAllWeights * GameData.rng.NextDouble();
			log.Verbose($"Random number in range 0 to {sumOfAllWeights} is {randomDouble}");
			int idx = 0;
			foreach (double cutoff in cutoffs) {
				if (randomDouble < cutoff) {
					log.Information($"Chose priority {possiblePriorities[idx]}");
					return possiblePriorities[idx];
				}
				idx++;
			}
			return new WarPriority();   //TODO: Fallback
		}
	}
}
