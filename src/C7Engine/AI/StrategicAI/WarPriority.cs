using System;
using System.Collections.Generic;
using C7Engine;
using C7Engine.AI.StrategicAI;

namespace C7GameData.AIData {
	/// <summary>
	/// Represents a goal of making war with another nation.
	/// Although I don't expect for this to be fully fleshed out by Carthage (diplomacy is way off in the future),
	/// having a priority that stores data is important for fleshing out how this is going to work, and this is an obvious
	/// case of a priority that will store data.
	/// </summary>
	public class WarPriority : StrategicPriority {

		private readonly int TEMP_WAR_PRIORITY_WEIGHT = 50; //temporary weight of this priority, if it isn't zero

		public WarPriority() {
			key = "WarPriority";
		}

		/// <summary>
		/// For now, we're simply going to say if we've run out of room for expansion, we'll fight someone.
		/// As we add more elements to the game, this should get more complex, as things like science and industry are considered.
		/// </summary>
		/// <param name="player"></param>
		/// <returns></returns>
		public override void CalculateWeightAndMetadata(Player player) {
			if (player.cities.Count < 2) {
				this.calculatedWeight = 0;
			} else {
				int landScore = UtilityCalculations.CalculateAvailableLandScore(player);
				//N.B. Eventually this won't be an all-or-nothing proposition; if land is getting tight but not quite zero,
				//the AI may decide it's time for the next phrase of the game, especially if it's aggressive.

				//nowhere else to expand
				if (landScore == 0) {
					//Figure out who to fight.  This should obviously be more sophisticated and should favor reachable opponents.
					//However, we don't yet store info on who's been discovered, so for now we'll choose someone randomly
					int opponentCount = EngineStorage.gameData.players.Count - 1;
					foreach (Player nation in EngineStorage.gameData.players) {
						if (nation != player) {
							int rnd = GameData.rng.Next(opponentCount);
							if (rnd == 0) {
								//Let's fight this nation!
								properties["opponent"] = nation.id.ToString();
								calculatedWeight = TEMP_WAR_PRIORITY_WEIGHT;
							} else {
								opponentCount--;    //guarantees we'll eventually get an opponent selected
							}
						}
					}
				}
			}
		}
	}
}
