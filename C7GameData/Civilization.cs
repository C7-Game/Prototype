using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace C7GameData
{
	/**
	 * Represents a civilization, such as the French, which can be
	 * assigned to a player.
	 */
	public enum Gender {
		Male,
		Female,
	}

	public class Civilization
	{
		public Civilization() {}

		public Civilization(string name) {
			this.name = name;
		}
		public string name;
		public string leader;
		public int color;
		public Gender leaderGender;
		public List<string> cityNames = new List<string>();
	}
}
