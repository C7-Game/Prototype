
using System.Collections.Generic;
using C7GameData;

namespace C7Engine.AI.StrategicAI {

	/// <summary>
	/// For now, this is an area where methods shared between multiple strategic AI classes can live.
	/// The structure of this may change over time...
	/// </summary>
	public class UtilityCalculations {

		private static readonly int POSSIBLE_CITY_LOCATION_SCORE = 2;   //how much weight to give to each possible city location
		private static readonly int TILE_SCORE_DIVIDER = 10;    //how much to divide each location's tile score by

		public static int CalculateAvailableLandScore(Player player) {
			//Figure out if there's land to settle, and how much
			Dictionary<Tile, int> possibleLocations = SettlerLocationAI.GetScoredSettlerCandidates(player.cities[0].location, player);
			int score = possibleLocations.Count * POSSIBLE_CITY_LOCATION_SCORE;
			foreach (int i in possibleLocations.Values) {
				score += i / TILE_SCORE_DIVIDER;
			}
			return score;
		}
	}
}
