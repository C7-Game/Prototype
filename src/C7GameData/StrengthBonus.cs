namespace C7GameData {
	using System.Collections.Generic;

	public struct StrengthBonus {
		public string description;
		public double amount; // e.g. 0.25 = +25% strength

		// Converts a list of strength bonuses to a multiplier on strength. For example, a list of two bonuses of +10% and +25% would return a
		// multiplier of 1.35. Multipliers cannot be less than zero.
		public static double ListToMultiplier(IEnumerable<StrengthBonus> bonuses) {
			double m = 1.0;
			foreach (StrengthBonus bonus in bonuses)
				m += bonus.amount;
			return (m >= 0.0) ? m : 0.0;
		}
	}
}
