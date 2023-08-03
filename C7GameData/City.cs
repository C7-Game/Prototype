using System;
using System.Collections.Generic;
using Serilog;

namespace C7GameData
{
    public class City
    {
        public ID id {get;}
        public Tile location {get;}
        public string name;
        public int size = 1;

        //Temporary production code because production is fun.
        public IProducible itemBeingProduced;
        public int shieldsStored = 0;

        public int foodStored = 0;
        public int foodNeededToGrow = 20;

        public bool capital = false;
        public Player owner {get; set;}
        public List<CityResident> residents = new List<CityResident>();

        public static City NONE = new City(Tile.NONE, null, "Dummy City", ID.None("city"));

        public City(Tile location, Player owner, string name, ID id)
        {
            this.id = id;
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
            return capital;
        }

        public bool CanBuildUnit(UnitPrototype proto)
        {
			List<string> allowedUnits = new List<string> {"Warrior", "Chariot", "Settler", "Worker", "Catapult", "Galley"};
			if (!allowedUnits.Contains(proto.name))
				return false;
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
	        else if (foodStored < 0) {
		        size--;
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
            if (shieldsStored >= itemBeingProduced.shieldCost && size > itemBeingProduced.populationCost) {
                shieldsStored = 0;
                size -= itemBeingProduced.populationCost;
                return itemBeingProduced;
            }

            shieldsStored = Math.Min(shieldsStored, itemBeingProduced.shieldCost);
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

		private void RemoveCitizen() {
			residents[residents.Count - 1].tileWorked.personWorkingTile = null;
			residents.RemoveAt(residents.Count - 1);
		}

		public void RemoveCitizens(int number) {
			for (int i = 0; i < number; i++) {
				if (residents.Count > 0) {
					RemoveCitizen();
				} else {
					Log.Warning("Trying to remove last citizen from " + name);
					break;
				}
			}
		}

		public void RemoveAllCitizens() {
			while (residents.Count > 0) {
				RemoveCitizen();
			}
		}

		public override string ToString() {
			return $"{name} ({size})";
		}
    }
}
