using System.Linq;
using Serilog;

namespace C7Engine
{
	using C7GameData;
	using System;
	using System.Collections.Generic;

	public class UnitInteractions
	{

		private static Queue<MapUnit> waitQueue = new Queue<MapUnit>();
		private static ILogger log = Log.ForContext<UnitInteractions>();

		public static MapUnit getNextSelectedUnit(GameData gameData)
		{
			foreach (Player player in gameData.players) {
				//TODO: Should pass in a player GUID instead of checking for human
				//This current limits us to one human player, although it's better
				//than the old limit of one non-barbarian player.
				if (player.isHuman) {
					foreach (MapUnit unit in player.units) {
						if (unit.movementPointsRemaining > 0 && !unit.IsBusy()) {
							if (!waitQueue.Contains(unit)) {
								return unit;
							}
						}
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
		 *
		 * TODO: It's kind of janky that the actions are being added to the unit.  They live on the unit prototype, and can be made available
		 * or unavailable based on game circumstances, e.g. technology or unit location.
		 * We probably *should* be returning just a list of the actions.  However, we're passing the result around via Godot signals, so
		 * I'm going to save that for a separate commit.
		 **/
		public static MapUnit UnitWithAvailableActions(MapUnit unit)
		{
			unit.availableActions.Clear();

			if (unit == MapUnit.NONE) {
				return unit;
			}

			// Eventually, we should look this up somewhere to see what all actions we have (and mods might add more)
			// For now, this is still an improvement over the last iteration.
			string[] implementedActions = { "hold", "wait", "fortify", "disband", "goTo", "buildCity", "bombard"};
			foreach (string action in implementedActions) {
				if (unit.unitType.actions.Contains(action)) {
					unit.availableActions.Add(action);
				}
			}

			// Eventually we will have advanced actions too, whose availability will rely on their base actions' availability.
			// unit.availableActions.Add("rename");

			return unit;
		}

		public static void ClearWaitQueue()
		{
			waitQueue.Clear();
		}

		public static void waitUnit(GameData gameData, string guid)
		{
			foreach (MapUnit unit in gameData.mapUnits)
			{
				if (unit.guid == guid)
				{
					log.Verbose("Found matching unit with guid " + guid + " of type " + unit.GetType().Name + "; adding it to the wait queue");
					waitQueue.Enqueue(unit);
				}
			}
			log.Warning("Failed to find a matching unit with guid " + guid);
		}
	}
}
