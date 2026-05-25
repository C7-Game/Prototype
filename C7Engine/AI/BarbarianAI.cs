using System.Collections.Generic;
using System.IO;
using System.Linq;
using Serilog;
using C7GameData.AIData;
using C7Engine.AI;
using C7Engine.AI.StrategicAI;
using C7Engine.AI.UnitAI;

namespace C7Engine {
	using C7GameData;
	using System;
	using System.Threading.Tasks;

	// TODO: The AI state (plans, strategy, ..) should be stored somewhere in game state.
	// For now, we have a stateless random AI.

	public static class BarbarianAI {

		private static ILogger log = Log.ForContext<TurnHandling>();

		public static async Task PlayTurn(Player player, GameData gameData) {
			if (!player.isBarbarians) {
				throw new Exception("Barbarian AI can only play barbarian players");
			}

			if (gameData.barbarianInfo.barbarianActivity == BarbarianActivity.None)
				return;

			var strategy = SelectStrategy(gameData.barbarianInfo.barbarianActivity);

			// TODO: Band units into tribes, decide at the tribe level --> work together

			foreach (MapUnit unit in player.units.ToArray()) {
				await strategy.PlayUnitTurn(player, unit);
				player.tileKnowledge.AddTilesToKnown(unit.location);
			}
		}

		private static IBarbarianStrategy SelectStrategy(BarbarianActivity barbarianActivity) {
			switch (barbarianActivity) {
				case BarbarianActivity.None:
					throw new Exception("Cannot select an AI strategy for BarbarianActivity 'None'.");
				case BarbarianActivity.Sedentary:
					return new SedentaryStrategy();
				case BarbarianActivity.Roaming:
					return new RoamingStrategy();
				case BarbarianActivity.Restless:
					return new RestlessStrategy();
				case BarbarianActivity.Raging:
					return new RagingStrategy();
				default:
					log.Warning("Unknown BarbarianActivity. Defaulting to Sedentary.");
					return new SedentaryStrategy();
			}
		}
	}
}
