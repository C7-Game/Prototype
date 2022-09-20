using System.Collections.Generic;

namespace C7GameData
{
    using System;
    public class City
    {
        public string guid {get;}
        public Tile location {get;}
        public string name;
        public int size = 1;

        //Temporary production code because production is fun.
        public IProducible itemBeingProduced;
        public int shieldsStored = 0;

        public int foodStored = 0;
        public int foodNeededToGrow = 20;

        public Player owner {get; set;}
        public List<CityResident> residents = new List<CityResident>();

        public static City NONE = new City(Tile.NONE, null, "Dummy City");

        public City(Tile location, Player owner, string name)
        {
            guid = Guid.NewGuid().ToString();
            this.location = location;
            this.owner = owner;
            this.name = name;
        }

        public void SetItemBeingProduced(IProducible producible)
        {
            this.itemBeingProduced = producible;
        }

        public bool IsCapital()
        {
            //TODO: Look through built cities, figure out if it is or not
            return true;
        }

        public bool CanBuildUnit(UnitPrototype proto)
        {
            if (proto.categories.Contains("Sea"))
                return location.NeighborsWater();
            else
                return true;
        }

        public int TurnsUntilGrowth() {
			if (FoodGrowthPerTurn() == 0) {
				return int.MaxValue;
			}
			int additionalFoodNeeded = foodNeededToGrow - foodStored;
            int turnsRoundedDown = additionalFoodNeeded / FoodGrowthPerTurn();
            if (additionalFoodNeeded % FoodGrowthPerTurn() != 0) {
                return turnsRoundedDown + 1;
            }
            return turnsRoundedDown;
        }

        public int TurnsToProduce(IProducible item) {
	        int additionalProductionNeeded = (item.shieldCost - shieldsStored);
            int turnsRoundedDown = additionalProductionNeeded / CurrentProductionYield();
            if (additionalProductionNeeded % CurrentProductionYield() != 0) {
                return turnsRoundedDown + 1;
            }
            return turnsRoundedDown;
        }

        public int TurnsUntilProductionFinished()
        {
            return TurnsToProduce(itemBeingProduced);
        }

        public void ComputeCityGrowth() {
	        foodStored += CurrentFoodYield() - size * 2;
	        if (foodStored >= foodNeededToGrow) {
		        size++;
		        foodStored = 0;
	        }
        }

        /**
         * Computes turn production.  If the production queue finishes,
         * returns the item that is built.  Otherwise, returns null.
         */
        public IProducible ComputeTurnProduction()
        {

			shieldsStored += CurrentProductionYield();
            if (shieldsStored >= itemBeingProduced.shieldCost) {
	            shieldsStored = 0;
	            if (itemBeingProduced.populationCost > 0) {
		            size -= itemBeingProduced.populationCost;
	            }
                return itemBeingProduced;
            }

            return null;
        }

		public int CurrentFoodYield()
		{
			int yield = 2;	//city center min yield
			foreach (CityResident r in residents) {
				yield += r.tileWorked.foodYield(owner);
			}
			return yield;
		}

		public int CurrentProductionYield()
		{
			int yield = 1;	//city center min yield
			foreach (CityResident r in residents) {
				yield += r.tileWorked.productionYield(owner);
			}
			return yield;
		}
		public int CurrentCommerceYield()
		{
			int yield = 3;	//city center min yield
			foreach (CityResident r in residents) {
				yield += r.tileWorked.commerceYield(owner);
			}
			return yield;
		}

		private int FoodGrowthPerTurn()
		{
			return CurrentFoodYield() - size * 2;
		}

		public override string ToString() {
			return $"{name} ({size})";
		}
    }
}
