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
				Console.WriteLine("Moving defender towards " + defenderAI.destination);
				
				Tile nextTile = defenderAI.pathToDestination.Next();
				unit.move(unit.location.directionTo(nextTile));
			}
			return true;
		}
	}
}
