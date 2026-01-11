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
		public enum Trait {
			Militaristic,
			Commercial,
			Expansionist,
			Scientific,
			Religious,
			Industrious,
			Agricultural,
			Seafaring,
		}

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

		// Like `art\advisors\LZ_all.pcx` for the English.
		public string leaderArtFile;

		public List<string> cityNames = new List<string>();

		// The IDs of all the techs that this civ starts with.
		public HashSet<ID> startingTechs = new();

		// The traits that this civilization has.
		public HashSet<Trait> traits = new();

		public bool isBarbarian = false;

		[JsonIgnore]
		public UnitPrototype uniqueUnit;

		private UnitPrototype GetDirectUpgrade(UnitPrototype unit) {
			// Check if a regular upgrade is replaced by a unique unit
			if (uniqueUnit != null
				&& uniqueUnit.unique.replace != null
				&& uniqueUnit.unique.replace == unit.upgradeTo) {
				return uniqueUnit;
			}

			return unit.upgradeTo;
		}

		public List<UnitPrototype> GetUpgradeChain(UnitPrototype unit) {
			List<UnitPrototype> result = [];
			var current = unit;

			while (true) {
				var upgrade = GetDirectUpgrade(current);
				if (upgrade == null) break;

				result.Add(upgrade);
				current = upgrade;
			}

			return result;
		}

		public bool IsUnitAvailable(UnitPrototype unit) {
			if (unit.unproducible) {
				return false;
			}

			// Check if unit is replaced by a unique unit
			if (uniqueUnit != null && uniqueUnit.unique.replace == unit) {
				return false;
			}

			// Check if unit is a unique unit from another civilization
			if (unit.unique != null && unit.unique.civilization != this) {
				return false;
			}

			return true;
		}
	}

}
