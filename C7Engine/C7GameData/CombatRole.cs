namespace C7GameData {
	using System;

	public enum CombatRole {
		Attack,
		Defense,
		Bombard,
		BombardDefense,
		DefensiveBombard,
		DefensiveBombardDefense
	}

	public static class CombatRoleExtensions {
		public static bool Attacking(this CombatRole cR) {
			return cR == CombatRole.Attack || cR == CombatRole.Bombard || cR == CombatRole.DefensiveBombard;
		}

		public static bool Defending(this CombatRole cR) {
			return !cR.Attacking();
		}

		public static bool Bombarding(this CombatRole cR) {
			return cR == CombatRole.Bombard || cR == CombatRole.DefensiveBombard;
		}

		public static CombatRole Reversed(this CombatRole cR) {
			switch (cR) {
				case CombatRole.Attack: return CombatRole.Defense;
				case CombatRole.Defense: return CombatRole.Attack;
				case CombatRole.Bombard: return CombatRole.BombardDefense;
				case CombatRole.BombardDefense: return CombatRole.Bombard;
				case CombatRole.DefensiveBombard: return CombatRole.DefensiveBombardDefense;
				case CombatRole.DefensiveBombardDefense: return CombatRole.DefensiveBombard;
				default: throw new ArgumentOutOfRangeException("Invalid CombatRole");
			}
		}
	}
}
