using System;
using C7GameData;
using C7GameData.AIData;

namespace C7Engine.AI {
	class DefenderAI : UnitAI {
		public bool PlayTurn(Player player, MapUnit unit) {
			DefenderAIData defenderAI = (DefenderAIData)unit.currentAIData;
			if (defenderAI.destination == unit.location) {
				if (!unit.isFortified) {
					unit.fortify();
					Console.WriteLine("Fortifying " + unit + " at " + defenderAI.destination);
				}
			}
			else {
				//TODO: Move towards destination
				Console.WriteLine("Moving defender towards " + defenderAI.destination);
			}
			return true;
		}
	}
}
