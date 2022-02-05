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
        public IProducable itemBeingProduced;
        public int shieldsStored = 0;
        public int shieldsPerTurn = 2;

        public int foodStored = 0;
        public int foodNeededToGrow = 20;
        public int foodGrowthPerTurn = 2;

        public Player owner {get; set;}

        public City(Tile location, Player owner, string name)
        {
            guid = Guid.NewGuid().ToString();
            this.location = location;
            this.owner = owner;
            this.name = name;
        }

        public void SetItemBeingProduced(IProducable producable)
        {
            this.itemBeingProduced = producable;
        }

        public bool IsCapital()
        {
            //TODO: Look through built cities, figure out if it is or not
            return true;
        }

        public int TurnsUntilGrowth() {
            int turnsRoundedDown = (foodNeededToGrow - foodStored) / foodGrowthPerTurn;
            if ((foodNeededToGrow - foodStored) % foodGrowthPerTurn != 0) {
                return turnsRoundedDown++;
            }
            return turnsRoundedDown;
        }

        public int TurnsUntilProductionFinished() {
            int turnsRoundedDown = (itemBeingProduced.shieldCost - shieldsStored) / shieldsPerTurn;
            if ((itemBeingProduced.shieldCost - shieldsStored) % shieldsPerTurn != 0) {
                return turnsRoundedDown++;
            }
            return turnsRoundedDown;
        }

        /**
         * Computes turn production.  Adjusts population if need be.  If the production queue finishes,
         * returns the item that is built.  Otherwise, returns null.
         */
        public IProducable ComputeTurnProduction()
        {
            foodStored+=foodGrowthPerTurn;
            if (foodStored >= foodNeededToGrow) {
                size++;
                foodStored = 0;
            }

            shieldsStored+=shieldsPerTurn;
            if (shieldsStored >= itemBeingProduced.shieldCost) {
	            shieldsStored = 0;
	            if (itemBeingProduced.populationCost > 0) {
		            size -= itemBeingProduced.populationCost;
	            }
                return itemBeingProduced;
            }

            return null;
        }
        
    }
}