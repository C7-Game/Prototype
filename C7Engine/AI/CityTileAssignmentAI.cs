using System;
using C7GameData;
using Serilog;

namespace C7Engine.AI
{
	public class CityTileAssignmentAI
	{
		public static int DesiredFoodSurplusPerTurn = 2;
		private static readonly int FOOD_PER_CITIZEN = 2;	//eventually will be configured by rules


		private static int foodPriorityRate = 40;
		private static int productionPriorityRate = 50;
		private static int commercePriorityRate = 30;

		private static ILogger log = Log.ForContext<CityTileAssignmentAI>();

		public static void AssignNewCitizenToTile(City city, CityResident newResident)
		{
			int foodYield = city.CurrentFoodYield();

			int desiredFoodRate = city.size * FOOD_PER_CITIZEN + DesiredFoodSurplusPerTurn;
			int targetTileFoodAmount = desiredFoodRate - foodYield;

			Tile cityCenter = city.location;

			double maxScore = 0;
			Tile preferredTile = Tile.NONE;
			foreach (Tile t in cityCenter.neighbors.Values) {
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
			log.Information($"Assigning new citizen of {city.name} to tile {preferredTile} with yield {yield}");

			newResident.tileWorked = preferredTile;
			preferredTile.personWorkingTile = newResident;

			city.residents.Add(newResident);
		}

		public static double CalculateTileYieldScore(Tile t, int targetFoodAmount, Player player)
		{
			int score = t.foodYield(player) * foodPriorityRate + t.productionYield(player) * productionPriorityRate + t.commerceYield(player) * commercePriorityRate;
			int penalty = (targetFoodAmount - t.foodYield(player));
			if (penalty <= 0) {
				return score;
			}
			return score / (Math.Pow(2, penalty));
		}
	}
}
