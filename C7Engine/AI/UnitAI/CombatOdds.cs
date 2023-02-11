using System.Collections.Generic;
using C7GameData;

namespace C7Engine.AI.UnitAI {
	/**
	 * This is the start of a combat odds calculator.
	 * Its primary purpose is to make the AI well-informed about the risks it is taking, so it can defeat the human
	 * more easily.
	 * Its secondary purpose is to allow an eventual Civ4-style UI giving the human a helpful tooltip about the odds,
	 * thus no longer placing the human at a disadvantage relative to the AI.
	 * This class will start out very simple, but over time we will add more complex scenarios, such as calculating
	 * odds per round of combat, and calculating the likely results of whole-stack combat, which will allow the AI
	 * to make smart decisions about how many units it needs to defeat human defenses.
	 */
	public class CombatOdds {
		public static double OddsOfVictory(MapUnit attacker, MapUnit defender) {
			//Yanked from MapUnitExtension's `fight` method
			IEnumerable<StrengthBonus> attackBonuses  = attacker.ListStrengthBonusesVersus(defender, CombatRole.Attack , attacker.facingDirection),
				defenseBonuses = defender.ListStrengthBonusesVersus(attacker, CombatRole.Defense, attacker.facingDirection);

			double attackerStrength = attacker.unitType.attack  * StrengthBonus.ListToMultiplier(attackBonuses),
				defenderStrength = defender.unitType.defense * StrengthBonus.ListToMultiplier(defenseBonuses);

			return attackerStrength / (attackerStrength + defenderStrength);
		}
	}
}
