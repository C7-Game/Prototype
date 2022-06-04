using System;
using System.Collections.Generic;
using C7Engine;

namespace C7GameData.AIData {
	/// <summary>
	/// Represents a goal of making war with another nation.
	/// Although I don't expect for this to be fully fleshed out by Carthage (diplomacy is way off in the future),
	/// having a priority that stores data is important for fleshing out how this is going to work, and this is an obvious
	/// case of a priority that will store data.
	/// </summary>
	public class WarPriority : StrategicPriority {

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
				int landScore = CalculateAvailableLandScore(player);
				if (landScore == 0) {	//nowhere else to expand
					//Figure out who to fight.  This should obviously be more sophisticated and should favor reachable opponents.
					//However, we don't yet store info on who's been discovered, so for now we'll choose someone randomly
					Random random = new Random();
					int opponentCount = EngineStorage.gameData.players.Count - 1;
					foreach (Player nation in EngineStorage.gameData.players)
					{
						if (nation != player) {
							int rnd = random.Next(opponentCount);
							if (rnd == 0) {
								//Let's fight this nation!
								properties["opponent"] = nation.guid;
								calculatedWeight = 50;
							} else {
								opponentCount--;	//guarantees we'll eventually get an opponent selected
							}
						}
					}
				}
			}
		}

		private static int CalculateAvailableLandScore(Player player)
		{
			//Figure out if there's land to settle, and how much
			Dictionary<Tile, int> possibleLocations = SettlerLocationAI.GetPossibleNewCityLocations(player.cities[0].location, player);
			int score = possibleLocations.Count * 10;
			foreach (int i in possibleLocations.Values) {
				score += i / 10;
			}
			return score;
		}
	}
}
