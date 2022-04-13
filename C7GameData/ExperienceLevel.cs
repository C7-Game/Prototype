namespace C7GameData
{
	using QueryCiv3.Biq;

	public class ExperienceLevel
	{
		public string name;
		public int baseHitPoints;
		public double retreatChance;

		public ExperienceLevel(string name, int baseHitPoints, double retreatChance)
		{
			this.name = name;
			this.baseHitPoints = baseHitPoints;
			this.retreatChance = retreatChance;
		}

		public static ExperienceLevel ImportFromCiv3(EXPR expr)
		{
			return new ExperienceLevel(expr.Name, expr.BaseHitPoints, expr.RetreatBonus / 100.0);
		}

		public static ExperienceLevel DEFAULT;
	}
}
