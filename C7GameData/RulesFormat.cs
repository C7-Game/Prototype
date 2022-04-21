namespace C7GameData

/*
    Rules are analagous to Civ3's BIQ and may exist as a standalone
    (e.g. in a mod) or as part of the save file. At least for now.
*/
{
	using System.Collections.Generic;
	using System.Text.Json.Serialization;

	public class C7RulesFormat {
		public string Version { get; set; }

		public List<ExperienceLevel> experienceLevels = new List<ExperienceLevel>();
		public string defaultExperienceLevelKey;
		[JsonIgnore]
		public ExperienceLevel defaultExperienceLevel;

		// TODO: Make sure these numbers are correct
		public double promotionChanceAfterDefending = 1.0/16;
		public double promotionChanceAfterAttacking = 1.0/8;

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
