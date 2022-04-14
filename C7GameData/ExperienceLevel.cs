namespace C7GameData
{
	using QueryCiv3.Biq;

	public class ExperienceLevel
	{
		public string key;
		public string displayName;
		public int baseHitPoints;
		public double retreatChance;

		public ExperienceLevel(string key, string displayName, int baseHitPoints, double retreatChance)
		{
			this.key = key;
			this.displayName = displayName;
			this.baseHitPoints = baseHitPoints;
			this.retreatChance = retreatChance;
		}

		public static ExperienceLevel ImportFromCiv3(string key, EXPR expr)
		{
			return new ExperienceLevel(key, expr.Name, expr.BaseHitPoints, expr.RetreatBonus / 100.0);
		}
	}
}
