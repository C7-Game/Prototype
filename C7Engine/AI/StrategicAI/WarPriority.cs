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
		public Dictionary<float, Dictionary<string, string>> GetWeight(Player player) {
			Dictionary<float, Dictionary<string, string>> returnValue = new Dictionary<float, Dictionary<string, string>>();
			if (player.cities.Count < 2) {
				returnValue[0] = new Dictionary<string, string>();
			} else {
				int landScore = CalculateAvailableLandScore(player);
				if (landScore == 0) {
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
								Dictionary<string, string> properties = new Dictionary<string, string>();
								properties["opponent"] = nation.guid;
								returnValue[50] = properties;
								return returnValue;
							}
						}
					}
				}
			}
			return returnValue;
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
