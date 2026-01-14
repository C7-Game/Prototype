using System.Linq;
using Serilog;

namespace C7Engine {
	using C7GameData;
	using System.Collections.Generic;

	public class UnitInteractions {

		private static Queue<MapUnit> waitQueue = new Queue<MapUnit>();
		private static ILogger log = Log.ForContext<UnitInteractions>();

		public static MapUnit getNextSelectedUnit() {
			foreach (Player player in EngineStorage.gameData.players.Where(p => p.isHuman)) {
				//TODO: Should pass in a player GUID instead of checking for human
				//This current limits us to one human player, although it's better
				//than the old limit of one non-barbarian player.
				foreach (MapUnit unit in player.units.Where(u => u.movementPoints.canMove)) {
					if (unit.isFortified) {
						continue;
					}

					if (unit.IsBusy()) {
						new MsgPerformUnitAction(unit).send();
						continue;
					}

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

		public static void ClearWaitQueue() {
			waitQueue.Clear();
		}

		public static void waitUnit(ID id) {
			foreach (MapUnit unit in EngineStorage.gameData.mapUnits) {
				if (unit.id == id) {
					log.Verbose("Found matching unit with id " + id + " of type " + unit.GetType().Name + "; adding it to the wait queue");
					waitQueue.Enqueue(unit);
				}
			}
			log.Warning("Failed to find a matching unit with id " + id);
		}
	}
}
