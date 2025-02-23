using System.Linq;
using Serilog;

namespace C7Engine {
	using C7GameData;
	using System.Collections.Generic;

	public class UnitInteractions {

		private static Queue<MapUnit> waitQueue = new Queue<MapUnit>();
		private static ILogger log = Log.ForContext<UnitInteractions>();

		public static MapUnit getNextSelectedUnit(GameData gameData) {
			foreach (Player player in gameData.players.Where(p => p.isHuman)) {
				//TODO: Should pass in a player GUID instead of checking for human
				//This current limits us to one human player, although it's better
				//than the old limit of one non-barbarian player.
				foreach (MapUnit unit in player.units.Where(u => u.movementPoints.canMove && !u.IsBusy())) {
					if (!waitQueue.Contains(unit)) {
						return unit;
					}
				}
			}
			if (waitQueue.Count > 0) {
				return waitQueue.Dequeue();
			}
			return MapUnit.NONE;
		}

		/**
		 * Helper function to add the available actions to a unit
		 * based on what terrain it is on.
		 **/
		public static List<string> GetAvailableActions(MapUnit unit) {
			List<string> result = new();
			if (unit == MapUnit.NONE) {
				return result;
			}

			// Eventually, we should look this up somewhere to see what all actions we have (and mods might add more)
			// For now, this is still an improvement over the last iteration.
			string[] implementedActions = { C7Action.UnitHold, C7Action.UnitWait, C7Action.UnitFortify, C7Action.UnitDisband, C7Action.UnitGoto, C7Action.UnitBombard };
			foreach (string action in implementedActions) {
				if (unit.unitType.actions.Contains(action)) {
					result.Add(action);
				}
			}

			if (unit.canBuildCity()) {
				result.Add(C7Action.UnitBuildCity);
			}

			if (unit.canBuildRoad()) {
				result.Add(C7Action.UnitBuildRoad);
			}

			if (unit.canBuildMine()) {
				result.Add(C7Action.UnitBuildMine);
			}

			if (unit.canIrrigate()) {
				result.Add(C7Action.UnitIrrigate);
			}

			// Eventually we will have advanced actions too, whose availability will rely on their base actions' availability.
			// unit.availableActions.Add("rename");

			return result;
		}

		public static void ClearWaitQueue() {
			waitQueue.Clear();
		}

		public static void waitUnit(GameData gameData, ID id) {
			foreach (MapUnit unit in gameData.mapUnits) {
				if (unit.id == id) {
					log.Verbose("Found matching unit with id " + id + " of type " + unit.GetType().Name + "; adding it to the wait queue");
					waitQueue.Enqueue(unit);
				}
			}
			log.Warning("Failed to find a matching unit with id " + id);
		}
	}
}
