using System.Collections.Generic;

namespace C7GameData
{
    using System;

    public class City : Unique
    {
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
            if (proto is SeaUnit)
                return location.NeighborsWater();
            else
                return true;
        }

        public int TurnsUntilGrowth() {
			if (FoodGrowthPerTurn() == 0) {
				return int.MaxValue;
			}
            int turnsRoundedDown = (foodNeededToGrow - foodStored) / FoodGrowthPerTurn();
            if ((foodNeededToGrow - foodStored) % FoodGrowthPerTurn() != 0) {
                return turnsRoundedDown++;
            }
            return turnsRoundedDown;
        }

        public int TurnsToProduce(IProducible item)
        {
            int turnsRoundedDown = (item.shieldCost - shieldsStored) / CurrentProductionYield();
            if ((item.shieldCost - shieldsStored) % CurrentProductionYield() != 0) {
                return turnsRoundedDown + 1;
            }
            return turnsRoundedDown;
        }

        public int TurnsUntilProductionFinished()
        {
            return TurnsToProduce(itemBeingProduced);
        }

        /**
         * Computes turn production.  Adjusts population if need be.  If the production queue finishes,
         * returns the item that is built.  Otherwise, returns null.
         */
        public IProducible ComputeTurnProduction()
        {
			foodStored += CurrentFoodYield() - size * 2;
            if (foodStored >= foodNeededToGrow) {
                size++;
                foodStored = 0;
            }

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
				yield += r.tileWorked.foodYield();
			}
			return yield;
		}

		public int CurrentProductionYield()
		{
			int yield = 1;	//city center min yield
			foreach (CityResident r in residents) {
				yield += r.tileWorked.productionYield();
			}
			return yield;
		}
		public int CurrentCommerceYield()
		{
			int yield = 3;	//city center min yield
			foreach (CityResident r in residents) {
				yield += r.tileWorked.commerceYield();
			}
			return yield;
		}

		private int FoodGrowthPerTurn()
		{
			return CurrentFoodYield() - size * 2;
		}
        
    }
}
