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

	public class CultureGroup() {
		public int index { get; init; }
		public string name { get; init; }
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
		// `adjective` is "American" for "America", or "Celtic" for "Celts", etc.
		public string adjective;
		public string leader;
		public int primaryColorIndex;
		public int secondaryColorIndex;
		public Gender leaderGender;

		[JsonIgnore]
		public CultureGroup cultureGroup { get; private set; }

		// This is null during gameplay to discourage usage, and instead use the cultureGroup
		// It's only used for saving/loading 
		public string cultureGroupKey;

		public void SetCultureGroup(int index, string cultureGroupName) {
			cultureGroupKey = null;
			var cg = new CultureGroup {
				index = index,
				name = cultureGroupName
			};
			cultureGroup = cg;
		}

		// Like `art\advisors\LZ_all.pcx` for the English.
		public string leaderArtFile;

		public List<string> cityNames = new List<string>();

		// The IDs of all the techs that this civ starts with.
		public HashSet<ID> startingTechs = new();

		// The traits that this civilization has.
		public HashSet<Trait> traits = new();

		public bool isBarbarian = false;

		public class SettlerTileAdjustments {
			public int DistancePenaltyRadius = 4;

			// TODO: Eventually, there should be different weights based on whether the AI already
			// has the resource or not (more important to secure ones that they don't have).
			// But since we don't have trade networks yet, for now there's only one value.

			// multipliers
			public float CommerceYieldBonus = 2;
			public float DistancePenalty = -2;
			public float FoodYieldBonus = 5;
			public float ProductionYieldBonus = 3;

			// constants
			public float HillsBonus = 10;
			public float LuxuryResourceBonus = 15;
			public float StrategicResourceBonus = 20;
			public float WaterBonus = 10;
		}

		public SettlerTileAdjustments Adjustments = new();

		// This method is primarily here to satisfy the weird upgrade chains from the .biq and .sav files
		public List<UnitPrototype> GetUpgradeChain(UnitPrototype unit) {
			List<UnitPrototype> result = [];
			var current = unit;

			while (true) {
				var upgrade = current.upgradeTo;
				if (upgrade == null) break;

				var upgradeIsAvailable = upgrade.producibleBy.Contains(this) && !result.Contains(upgrade);

				if (upgradeIsAvailable) {
					result.Add(upgrade);
				}
				current = upgrade;

			}

			for (int i = result.Count - 1; i >= 0; i--) {
				if (!result[i].producibleBy.Contains(this)) {
					result.Remove(result[i]);
				}
			}

			return result;
		}

		public bool IsUnitAvailable(UnitPrototype unit) {
			if (unit.unproducible) {
				return false;
			}

			if (!unit.producibleBy.Contains(this)) {
				return false;
			}

			return true;
		}
	}

}
