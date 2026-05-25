using System;
using System.Collections.Generic;
using C7Engine;
using C7Engine.AI.StrategicAI;
using Serilog;

namespace C7GameData.AIData {
	public class ExpansionPriority : StrategicPriority {
		private readonly int TEMP_GAME_LENGTH = 540;
		private readonly int EARLY_GAME_CUTOFF = 25;    //what percentage of the game is early game, which should give expansion a boost?

		private static readonly int SETTLER_FLAT_APPEAL = 30;           //the base "flat" appeal of settler-type units
		private static readonly float SETTLER_WEIGHTED_APPEAL = 4.0f;   //the multiplier effect on settler-type units

		private ILogger log = Log.ForContext<ExpansionPriority>();

		public ExpansionPriority() { }

		public override void CalculateWeightAndMetadata(Player player) {
			if (player.cities.Count < 2) {
				this.calculatedWeight = 1000;
			} else {
				float score = UtilityCalculations.CalculateAvailableLandScore(player);
				score = ApplyEarlyGameMultiplier(score);
				score = ApplyNationTraitMultiplier(score, player);

				this.calculatedWeight = score;
			}
		}

		public override float GetProductionItemFlatAdjuster(Player player, IProducible producible) {
			if (producible is UnitPrototype prototype) {
				if (prototype.actions.Contains(UnitAction.BuildCity)) {
					//Offset the shield cost and pop cost maluses, and add a flat 30 value to be equivalent to an early-game unit
					int adjustment = player.ShieldCost(prototype) + 10 * prototype.populationCost + SETTLER_FLAT_APPEAL;
					log.Debug($"ExpansionPriority adjusting {producible} by {adjustment}");
					return adjustment;
				}
			}
			return 0.0f;
		}

		/// <summary>
		/// This priority will prefer units that can build cities.
		/// </summary>
		/// <param name="producible"></param>
		/// <returns></returns>
		public override float GetProductionItemPreferenceWeight(IProducible producible) {
			if (producible is UnitPrototype prototype) {
				if (prototype.actions.Contains(UnitAction.BuildCity)) {
					return SETTLER_WEIGHTED_APPEAL;
				}
			}
			return 0.0f;
		}

		public override string ToString() {
			return "ExpansionPriority";
		}

		private float ApplyEarlyGameMultiplier(float score) {
			//If it's early game, multiply this score.
			//TODO: We haven't implemented the part for "how many turns does the game have?" yet.  So this is hard-coded.
			var gameTurn = EngineStorage.gameData.turn;
			var percentOfGameFinished = (gameTurn * 100) / TEMP_GAME_LENGTH;
			var isEarlyGame = percentOfGameFinished < EARLY_GAME_CUTOFF;
			var multiplier = isEarlyGame ? (EARLY_GAME_CUTOFF - percentOfGameFinished) / 5.0f : 1.0f;
			return score * multiplier;
		}

		private float ApplyNationTraitMultiplier(float score, Player player) {
			// TODO: The "Expansionist" trait should give a higher priority to this strategic priority.
			return score;
		}
	}
}
