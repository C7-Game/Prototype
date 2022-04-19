namespace C7GameData

/*
    Rules are analagous to Civ3's BIQ and may exist as a standalone
    (e.g. in a mod) or as part of the save file. At least for now.
*/
{
	using System.Collections.Generic;
	using System.Text.Json.Serialization;

	public struct StrengthBonus
	{
		public string description;
		public double amount; // e.g. 0.25 = +25% strength

		// Converts a list of strength bonuses to a multiplier on strength. For example, a list of two bonuses of +10% and +25% would return a
		// multiplier of 1.35. Multipliers cannot be less than zero.
		public static double ListToMultiplier(IEnumerable<StrengthBonus> bonuses)
		{
			double m = 1.0;
			foreach (StrengthBonus bonus in bonuses)
				m += bonus.amount;
			return (m >= 0.0) ? m : 0.0;
		}
	}

	public class C7RulesFormat
	{
		public string Version { get; set; }

		public List<ExperienceLevel> experienceLevels = new List<ExperienceLevel>();
		public string defaultExperienceLevelKey;
		[JsonIgnore]
		public ExperienceLevel defaultExperienceLevel;

		// TODO: Make sure these numbers are correct
		public double promotionChanceAfterDefending = 1.0/16;
		public double promotionChanceAfterAttacking = 1.0/8;

		public StrengthBonus fortificationBonus;
		public StrengthBonus riverCrossingBonus;
		public StrengthBonus cityLevel1DefenseBonus;
		public StrengthBonus cityLevel2DefenseBonus;
		public StrengthBonus cityLevel3DefenseBonus;

		public int healRateInField;
		public int healRateInCity;

		public C7RulesFormat() {
			Version = "v0.0early-prototype";
		}

		public ExperienceLevel GetExperienceLevelAfter(ExperienceLevel experienceLevel)
		{
			int n = experienceLevels.IndexOf(experienceLevel);
			if (n + 1 < experienceLevels.Count)
				return experienceLevels[n + 1];
			else
				return null;
		}
	}
}
