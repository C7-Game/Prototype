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

		public C7RulesFormat() {
			Version = "v0.0early-prototype";
		}
	}
}
