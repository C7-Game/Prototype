using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace C7GameData {
	/**
	 * Represents a civilization, such as the French, which can be
	 * assigned to a player.
	 */
	public enum Gender {
		Male,
		Female,
	}

	public class Civilization {
		public Civilization() { }

		public Civilization(string name) {
			this.name = name;
		}
		public string name;

		// `noun` is "Americans" for "America", or "Spanish" for "Spain", etc.
		public string noun;
		public string leader;
		public int colorIndex;
		public Gender leaderGender;
		public List<string> cityNames = new List<string>();

		// The IDs of all the techs that this civ starts with.
		public HashSet<ID> startingTechs = new();
	}
}
