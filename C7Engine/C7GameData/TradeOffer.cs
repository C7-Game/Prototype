
using System.Collections.Generic;

namespace C7GameData {
	// A class representing one side of a diplomatic agreement between two civs.
	public class TradeOffer {
		// True if two players involved in the agreement were at war prior to
		// the agreement.
		public bool partOfPeaceTreaty = false;

		public int? gold = null;
		public List<Tech> techs = new();

		// Calculate how much this trade offer is worth for a given player. This
		// has to be per-player because tech costs vary based on how many civs
		// a player have already researched the tech.
		public int GoldEquivalentFor(GameData gameData, Player p) {
			int result = 0;
			if (gold.HasValue) {
				result += gold.Value;
			}
			foreach (Tech t in techs) {
				result += gameData.TechCostFor(t, p);
			}

			return result;
		}

		public void Clear() {
			partOfPeaceTreaty = false;
			gold = null;
			techs.Clear();
		}

		public string ToString() {
			List<string> pieces = new();
			if (partOfPeaceTreaty) {
				pieces.Add("peace treaty");
			}
			if (gold != null) {
				pieces.Add($"{gold.Value} gold");
			}
			foreach (Tech t in techs) {
				pieces.Add(t.Name);
			}
			return string.Join(",", pieces);
		}
	}
}
