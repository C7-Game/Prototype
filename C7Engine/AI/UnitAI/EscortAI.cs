using System;
using C7Engine.Pathing;
using C7GameData;
using System.Collections.Generic;
using System.Linq;
using C7GameData.AIData;
using C7Engine.AI;
using Serilog;

namespace C7Engine {
	public class EscortAI : UnitAI {
		private static ILogger log = Log.ForContext<EscortAI>();
		public EscortAIData data;

		public EscortAI(EscortAIData d) {
			data = d;
		}

		public void UpdateOnDeath() {
			if (data.unitToEscort == null) {
				return;
			}

			// When we're destroyed, clear out our reference to the unit we're
			// escorting, and clear our their reference to us.
			if (data.unitToEscort.currentAI is SettlerAI settlerAi) {
				settlerAi.data.escort = null;
			}
			data.unitToEscort = null;
		}

		public static EscortAIData? MaybeMakeAiData(MapUnit unit, Player player) {
			// Ensure we don't have a boat trying to escort a settler.
			if (!unit.CanDefendOnLand()) {
				return null;
			}

			foreach (MapUnit u in unit.location.unitsOnTile) {
				if (u.currentAI is SettlerAI settlerAi && settlerAi.data.escort == null) {
					settlerAi.data.escort = unit;
					EscortAIData result = new();
					result.unitToEscort = u;
					log.Information($"Having {unit.id} {unit} escort {u.id}, {u}");
					return result;
				}
			}

			// TODO: Check if there are nearby units to escort.
			return null;
		}

		public string SummarizePlan() {
			return "EscortAI: " + data.ToString();
		}

		UnitAI.MoveResult UnitAI.PlayTurnImpl(Player player, MapUnit unit) {
			if (data == null || data.unitToEscort == null || data.unitToEscort.currentAI == null) {
				return UnitAI.Result.Error;
			}

			// Ensure the unit we're escorting has moved, in case we were first
			// in the unit ordering.
			UnitAI.MoveResult result = data.unitToEscort.currentAI.PlayTurn(player, data.unitToEscort).Result;
			if (result == UnitAI.Result.Done) {
				return result;
			}
			if (result == UnitAI.Result.Error) {
				// If there was an error clear out their AI logic. This would
				// happen if the unit was moving on its own in PlayerAI, but
				// because we're moving for them, we have to do it here.
				data.unitToEscort.currentAI = null;
				return result;
			}

			// If we're on the correct location, give up the rest of our MPs.
			if (unit.location == data.unitToEscort.location) {
				unit.movementPoints.onConsumeAll();
				return UnitAI.Result.InProgress;
			}

			// Move to the unit we're escorting.
			TilePath path = PathingAlgorithmChooser.GetAlgorithm(unit).PathFrom(unit.location, data.unitToEscort.location, unit);
			result = this.TryToMoveAlongPath(unit, ref path, allowCombat: false);
			if (result != UnitAI.Result.InProgress) {
				return result;
			}

			// If we're on the correct location, give up the rest of our MPs.
			if (unit.location == data.unitToEscort.location) {
				unit.movementPoints.onConsumeAll();
			}
			return UnitAI.Result.InProgress;
		}
	}
}
