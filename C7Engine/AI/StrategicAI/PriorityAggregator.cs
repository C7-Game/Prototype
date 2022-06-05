using System;
using System.Collections.Generic;
using C7GameData.AIData;

namespace C7Engine.AI.StrategicAI {
	/// <summary>
	/// Eventually this will be a sophisticated way to look up all registered strategic priorities, including from DLLs or the equivalent.
	/// For now, it's hard-coded, but that's okay for the time being.
	/// </summary>
	public class PriorityAggregator {

		/// <summary>
		/// Returns a list of all strategic priorities registered in the game.
		///
		/// This returns the type, so you can create an instance and then call methods on them.
		/// This follows what appears to be the C# equivalent of using Java class objects, useful for things like making annotation processors.
		/// In this case, I'm intentionally following that paradigm because by doing so, this method can be enhanced to look for additional
		/// types registered by mods, or simply stored in different components (DLLs/JARs/whatever the .NET equivalent is).
		/// </summary>
		/// <returns></returns>
		public static List<Type> GetAllStrategicPriorityTypes() {
			List<Type> priorities = new List<Type>();
			priorities.Add(typeof(ExpansionPriority));
			priorities.Add(typeof(ExplorationPriority));
			return priorities;
		}
	}
}
