using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
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
			List<Type> priorityTypes = StrategicPriority.GetAllStrategicPriorityTypes();
			List<StrategicPriority> possiblePriorities = new List<StrategicPriority>();

			foreach (Type priorityType in priorityTypes) {
				//Need to create an instance
				ConstructorInfo constructor = priorityType.GetConstructor(Type.EmptyTypes);
				object instance = constructor.Invoke(Array.Empty<object>());
				StrategicPriority priority = (StrategicPriority)instance;

				MethodInfo GetWeightAndMetadata = priorityType.GetMethod("GetWeight", new Type[] { typeof(Player)});
				//TODO: This doesn't return a type anymore, or shouldn't anyway
				Object result = GetWeightAndMetadata.Invoke(priority, new object[] {player});

				//We need to store these somewhere for each type.  Do we want to store it based on the instance?
				possiblePriorities.Add(priority);
			}

			//Now we've got all the data, it's time to choose between the options
			PrioritizationType weighting = PrioritizationType.WEIGHTED_QUADRATIC;
			//We need to construct ranges and stuff
			double sumOfAllWeights = 0.0;
			List<double> cutoffs = new List<double>();
			foreach (StrategicPriority possiblePriority in possiblePriorities) {
				double weightedWeight = possiblePriority.GetCalculatedWeight() * possiblePriority.GetCalculatedWeight();
				double oldCutoff = sumOfAllWeights;
				sumOfAllWeights += weightedWeight;

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
			//should never get here
			return possiblePriorities[0];
		}
	}
}
