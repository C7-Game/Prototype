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

	public class BarbarianAI {

		private ILogger log = Log.ForContext<BarbarianAI>();

		public async Task PlayTurn(Player player, GameData gameData) {
			if (!player.isBarbarians) {
				throw new System.Exception("Barbarian AI can only play barbarian players");
			}

			foreach (MapUnit unit in player.units.ToArray()) {
				// Make the barbarians wake up if they see a unit or a civ's
				// borders. This will happen each turn, so eventually the barb
				// should muster the courage to attack.
				foreach (Tile t in unit.location.neighbors.Values) {
					if (t.unitsOnTile.Count > 0 && t.unitsOnTile[0].owner != player) {
						unit.wake();
						break;
					}
					if (t.OwningPlayer() != null) {
						unit.wake();
						break;
					}
				}

				// Don't waste time recalculating behaviors for fortified units.
				if (unit.isFortified) {
					continue;
				}

				// For each unit, if there's already an AI task assigned, it will attempt to complete its goal.
				// It may fail due to conditions having changed since that goal was assigned; in that case it will
				// get a new task to try to complete.
				//
				// Cap our attempts at 2 to avoid getting stuck in bad situations.
				for (int attempt = 0; attempt < 2; ++attempt) {
					if (unit.currentAI == null) {
						unit.currentAI = GetAIForUnit(unit, player);
					}

					// If the unit is still the process of doing its plan, allow
					// it to continue next turn.
					UnitAI.Result result = await unit.currentAI.PlayTurn(player, unit);
					if (result == UnitAI.Result.InProgress) {
						break;
					}

					if (result == UnitAI.Result.Error) {
						unit.currentAI = null;
						break;
					}

					if (unit.hitPointsRemaining <= 0 || unit.isFortified) {
						unit.currentAI = null;
						break;
					}

					// Otherwise we need a new plan for next turn. Pick it now
					// to avoid things like new units being preferred for
					// exploration instead of units already far away from home
					// for exploration.
					unit.currentAI = GetAIForUnit(unit, player);
				}

				player.tileKnowledge.AddTilesToKnown(unit.location);
			}
		}

		public static UnitAI GetAIForUnit(MapUnit unit, Player player) {
			// Barbarians should always defend their camp if it is unguarded.
			if (unit.location.hasBarbarianCamp && unit.location.unitsOnTile.Count == 1) {
				return new DefenderAI(DefenderAI.MakeAiDataForDefendInPlace(unit, player));
			}

			// If the barbarian can fight, it should.
			CombatAIData maybeCombat = CombatAI.MakeAiData(unit, player);
			if (maybeCombat != null) {
				return new CombatAI(maybeCombat);
			}

			// Give barbarians a chance to explore if they can't fight.
			if (GameData.rng.Next(100) < 30) {
				ExplorerAIData? maybeAiData = ExplorerAI.MaybeMakeAiData(unit, player);
				if (maybeAiData != null) {
					return new ExplorerAI(maybeAiData);
				}
			}

			// Otherwise just sit tight.
			return new DefenderAI(DefenderAI.MakeAiDataForDefendInPlace(unit, player));
		}
	}
}
