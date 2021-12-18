namespace C7GameData
{
    using System;
    public class City
    {
        public string guid {get;}
        public int xLocation {get;}
        public int yLocation {get;}
        public string name;
        public int size = 1;

        //Temporary production code because production is fun.
        public string itemBeingProduced = "Warrior";
        public int shieldCost = 10;
        public int shieldsStored = 0;
        public int shieldsPerTurn = 2;

        public int foodStored = 0;
        public int foodNeededToGrow = 20;
        public int foodGrowthPerTurn = 2;

        public Player owner {get; set;}

        public City(int x, int y, Player owner, string name)
        {
            guid = Guid.NewGuid().ToString();
            this.xLocation = x;
            this.yLocation = y;
            this.owner = owner;
            this.name = name;
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
            int turnsRoundedDown = (shieldCost - shieldsStored) / shieldsPerTurn;
            if ((shieldCost - shieldsStored) % shieldsPerTurn != 0) {
                return turnsRoundedDown++;
            }
            return turnsRoundedDown;
        }

        //Placeholder for now.  Don't be alarmed that it ignores things like the produce-next popup
        //Probably don't want to return a string here.  Just doing things the wrong way to add behavior quickly so Babylon is more fun.
        public string ComputeTurnProduction()
        {
            string itemProduced = "";

            foodStored+=foodGrowthPerTurn;
            if (foodStored >= foodNeededToGrow) {
                size++;
                foodStored = 0;
            }

            shieldsStored+=shieldsPerTurn;
            if (shieldsStored >= shieldCost) {
                itemProduced = itemBeingProduced;
                shieldsStored = 0;
                if (itemProduced == "Warrior") {
                    itemBeingProduced = "Chariot";
                    shieldCost = 20;
                }
                else if (itemProduced == "Chariot") {
                    itemBeingProduced = "Settler";
                    shieldCost = 30;
                }
                else if (itemProduced == "Settler") {
                    size -= 2;
                    itemBeingProduced = "Warrior";
                    shieldCost = 10;
                }
            }

            return itemProduced;
        }
    }
}