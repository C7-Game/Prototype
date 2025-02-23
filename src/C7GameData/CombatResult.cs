namespace C7GameData {
	public enum CombatResult {
		AttackerKilled,
		DefenderKilled,
		AttackerRetreated,
		DefenderRetreated,
		Impossible
	}

	public static class CombatResultExtensions {
		public static bool AttackerWon(this CombatResult cR) {
			return (cR == CombatResult.DefenderKilled) || (cR == CombatResult.DefenderRetreated);
		}

		public static bool DefenderWon(this CombatResult cR) {
			return (cR == CombatResult.AttackerKilled) || (cR == CombatResult.AttackerRetreated);
		}
	}
}
