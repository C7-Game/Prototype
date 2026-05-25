
using System.Collections.Generic;
using System.Linq;
using C7GameData;

namespace C7Engine.AI.StrategicAI {

	/// <summary>
	/// For now, this is an area where methods shared between multiple strategic AI classes can live.
	/// The structure of this may change over time...
	/// </summary>
	public class UtilityCalculations {

		private static readonly int PossibleCityLocationScore = 2;   //how much weight to give to each possible city location
		private static readonly float TileScoreDivider = 10f;    //how much to divide each location's tile score by

		public static float CalculateAvailableLandScore(Player player) {
			//Figure out if there's land to settle, and how much
			var possibleLocations = SettlerLocationAI.GetScoredSettlerCandidates(player.cities[0].location, player);
			var availableLand = possibleLocations.Count * PossibleCityLocationScore;
			var settlementQuality = possibleLocations.Values.Sum(i => i / TileScoreDivider);
			return settlementQuality + availableLand;
		}
	}
}
