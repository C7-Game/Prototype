using System;
using C7GameData;
using C7GameData.AIData;
using Serilog;

namespace C7Engine.AI {
	class DefenderAI : UnitAI {

		private ILogger log = Log.ForContext<DefenderAI>();

		public bool PlayTurn(Player player, MapUnit unit) {
			DefenderAIData defenderAI = (DefenderAIData)unit.currentAIData;
			if (defenderAI.destination == unit.location) {
				if (!unit.isFortified) {
					unit.fortify();
					log.Information("Fortifying " + unit + " at " + defenderAI.destination);
				}
			} else {
				log.Debug("Moving defender towards " + defenderAI.destination);

				Tile nextTile = defenderAI.pathToDestination.Next();
				if (nextTile != Tile.NONE) {
					unit.move(unit.location.directionTo(nextTile));
				} else {
					//Got a crash due to trying to move to (or less likely from) Tile.NONE.
					//However, from the logs, the destination was [15, 55], so somehow the path
					//included Tile.NONE.  The unit was an AI (Roman) unit; due to the crash I can't get more info
					//One possibility is it was blocked in its path the previous turn; could this happen if multiple
					//units were on a tile it was moving to, and it defeated one, but still couldn't move?  That would
					//likely affect its pathing.  Put a breakpoint here while debugging!
					//This should be a higher severity Serilog error
					log.Error("ERROR: Unit pathed via Tile.NONE");
				}
			}
			return true;
		}
	}
}
