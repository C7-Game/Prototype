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
        public int shieldCost = 10;
        public int shieldsStored = 0;
        public int shieldsPerTurn = 2;

        public int foodStored = 0;
        public int foodNeededToGrow = 20;
        public int foodGrowthPerTurn = 2;

        public Player owner {get; set;}
        
        public delegate void OnUnitCompletedDelegate(MapUnit newUnit);
        private OnUnitCompletedDelegate onUnitCompleted;

        public delegate IProducable GetNextItemToBeProducedDelegate(City city, IProducable lastItemProduced);
        private GetNextItemToBeProducedDelegate getNextItemToBeProduced;

        public City(Tile location, Player owner, string name, OnUnitCompletedDelegate ouc, GetNextItemToBeProducedDelegate gnitbpd)
        {
            guid = Guid.NewGuid().ToString();
            this.location = location;
            this.owner = owner;
            this.name = name;
            this.onUnitCompleted = ouc;
            getNextItemToBeProduced = gnitbpd;
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
				if (itemBeingProduced is UnitPrototype prototype) {
					MapUnit newUnit = prototype.GetInstance();
					newUnit.owner = this.owner;
					newUnit.location = this.location;
					newUnit.facingDirection = TileDirection.SOUTHWEST;
					
					location.unitsOnTile.Add(newUnit);
					
					//Figuring out the paradigms here.  We're sorta object-oriented,
					//but we're also trying to not box ourselves in to not being able to
					//be client-server.  C7GameData is a child of C7Engine, so the engine
					//has to add the unit to the list.
					//Probably the engine should be doing most of the other stuff, too.
					onUnitCompleted(newUnit);
					
					//In reality, the human would set the next produced unit (unless it's an AI)
					//For now I'm going to figure something out that gets a new unit, but is more
					//engine/AI level
					itemBeingProduced = getNextItemToBeProduced(this, prototype);
				}
				else {
				    //building.  adding later
				}
                shieldsStored = 0;
            }

            return itemProduced;
        }
    }
}