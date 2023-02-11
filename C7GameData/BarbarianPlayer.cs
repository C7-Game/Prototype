using System;
using System.Collections.Generic;

namespace C7GameData {
	/**
	 * Special class for the barbarian player.
	 * They have some unique behavior, notably being organized by tribe.
	 */
	public class BarbarianPlayer : Player {
		private List<BarbarianTribe> tribes = new List<BarbarianTribe>();

		public BarbarianPlayer(uint color) {
			guid = Guid.NewGuid().ToString();
			this.color = (int)(color & 0xFFFFFFFF);
			this.isBarbarians = true;
		}

		public void AddTribe(Tile tile, MapUnit startingUnit) {
			tribes.Add(new BarbarianTribe(tile, startingUnit));
		}
	}
}
