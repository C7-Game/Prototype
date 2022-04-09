using System;
using C7GameData;

namespace C7Engine.AI
{
	public class CityTileAssignmentAI
	{
		public static int DESIRED_FOOD_SURPLUS_PER_TURN = 2;
		private static readonly int FOOD_PER_CITIZEN = 2;	//eventually will be configured by rules

		
		private static int foodPriorityRate = 40;
		private static int productionPriorityRate = 50;
		private static int commercePriorityRate = 30;
		
		public static void AssignNewCitizenToTile(City city, CityResident newResident)
		{
			int foodYield = city.CurrentFoodYield();

			int desiredFoodRate = city.size * FOOD_PER_CITIZEN + DESIRED_FOOD_SURPLUS_PER_TURN;
			int targetTileFoodAmount = desiredFoodRate - foodYield;
			
			Tile cityCenter = city.location;

			double maxScore = 0;
			Tile preferredTile = Tile.NONE;
			foreach (Tile t in cityCenter.neighbors.Values) {
				double score = CalculateTileYieldScore(t, targetTileFoodAmount);
				Console.WriteLine($"Tile {t} scored {score}");
				if (score > maxScore) {
					maxScore = score;
					preferredTile = t;
				}
			}
			
			Console.WriteLine($"Assigning new citizen of {city.name} to tile {preferredTile} with yield {preferredTile.foodYield()}/{preferredTile.productionYield()}/{preferredTile.commerceYield()}");

			newResident.tileWorked = preferredTile;
			preferredTile.personWorkingTile = newResident;
			
			city.residents.Add(newResident);
		}

		public static double CalculateTileYieldScore(Tile t, int targetFoodAmount)
		{
			int score = t.foodYield() * foodPriorityRate + t.productionYield() * productionPriorityRate + t.commerceYield() * commercePriorityRate;
			int penalty = (targetFoodAmount - t.foodYield());
			if (penalty <= 0) {
				return score;
			}
			return score / (Math.Pow(2, penalty));
		}
	}
}
