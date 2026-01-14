using System;
using C7GameData;
using System.Linq;
using Serilog;

namespace C7Engine.AI {
	public class CityTileAssignmentAI {
		public static int DesiredFoodSurplusPerTurn = 2;
		private static readonly int FOOD_PER_CITIZEN = 2;   //eventually will be configured by rules


		private static int foodPriorityRate = 40;
		private static int productionPriorityRate = 50;
		private static int commercePriorityRate = 30;

		private static ILogger log = Log.ForContext<CityTileAssignmentAI>();

		// Assigns a citizen, which is alredy part of a city, to a tile, if possible.
		public static void AssignNewCitizenToTile(GameData gameData, CityResident newResident, bool manageMoods = false) {
			City city = newResident.city;
			int foodYield = city.CurrentFoodYield();

			int desiredFoodRate = city.residents.Count * FOOD_PER_CITIZEN + DesiredFoodSurplusPerTurn;
			int targetTileFoodAmount = desiredFoodRate - foodYield;

			double maxScore = 0;
			Tile preferredTile = Tile.NONE;
			foreach (Tile t in city.GetWorkableTiles()) {
				if (t.personWorkingTile == null) {
					double score = CalculateTileYieldScore(t, targetTileFoodAmount, city.owner);
					log.Debug($"Tile {t} scored {score}");
					if (score > maxScore) {
						maxScore = score;
						preferredTile = t;
					}
				}
			}

			string yield = city.location.YieldString(city.owner);
			log.Debug($"Assigning new citizen of {city.name} to tile {preferredTile} with yield {yield}");

			if (preferredTile == Tile.NONE) {
				newResident.citizenType = city.owner.GetKnownSpecialists(gameData)[0];
			} else {
				newResident.tileWorked = preferredTile;
				preferredTile.personWorkingTile = newResident;
			}

			// Check to see if this citizen working a tile would cause the city
			// to be unhappy. If so, make them an entertainer.
			City.Mood cityMood = city.RecalculateCitizenMoods(gameData);

			if (cityMood == City.Mood.Unhappy && manageMoods) {
				newResident.citizenType = city.owner.GetKnownSpecialists(gameData).MaxBy(x => x.Luxuries);
				if (newResident.tileWorked != Tile.NONE) {
					newResident.tileWorked = Tile.NONE;
					preferredTile.personWorkingTile = null;
				}
			}
		}

		public static double CalculateTileYieldScore(Tile t, int targetFoodAmount, Player player) {
			int score = t.foodYield(player).yield * foodPriorityRate
				+ t.productionYield(player).yield * productionPriorityRate
				+ t.commerceYield(player).yield * commercePriorityRate;
			int penalty = (targetFoodAmount - t.foodYield(player).yield);
			if (penalty <= 0) {
				return score;
			}
			return score / (Math.Pow(2, penalty));
		}
	}
}
