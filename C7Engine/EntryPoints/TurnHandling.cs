using C7GameData.AIData;

namespace C7Engine
{
    using C7GameData;
    using System;
    public class TurnHandling
    {
        public static void EndTurn()
        {
            GameData gameData = EngineStorage.gameData;
            //Barbarians.  First, generate new barbarian units.
            foreach (Tile tile in gameData.map.barbarianCamps)
            {
                //7% chance of a new barbarian.  Probably should scale based on barbarian activity.  
                Random rnd = new Random();
                int result = rnd.Next(100);
                Console.WriteLine("Random barb result = " + result);
                if (result < 7) {
                    MapUnit newUnit = new MapUnit();
                    newUnit.location = tile;
                    newUnit.owner = gameData.players[0];
                    newUnit.unitType = gameData.unitPrototypes["Warrior"];
                    newUnit.hitPointsRemaining = 3;
                    newUnit.isFortified = true; //todo: hack for unit selection

                    tile.unitsOnTile.Add(newUnit);
                    gameData.mapUnits.Add(newUnit);
                    Console.WriteLine("New barbarian added at " + tile);
                }
                else if (tile.NeighborsCoast() && result < 10) {
                    MapUnit newUnit = new MapUnit();
                    newUnit.location = tile;
                    newUnit.owner = gameData.players[0];    //todo: make this reliably point to the barbs
                    newUnit.unitType = gameData.unitPrototypes["Galley"];
                    newUnit.hitPointsRemaining = 3;
                    newUnit.isFortified = true; //todo: hack for unit selection

                    tile.unitsOnTile.Add(newUnit);
                    gameData.mapUnits.Add(newUnit);
                    Console.WriteLine("New barbarian galley added at " + tile);
                }
            }
            //Call the barbarian AI
            //TODO: The AIs should be stored somewhere on the game state as some of them will store state (plans, strategy, etc.)
            //For now, we only have a random AI, so that will be in a future commit
            BarbarianAI barbarianAI = new BarbarianAI();
            barbarianAI.PlayTurn(gameData.players[0], gameData);

			//Non-Barbarian AIs
			foreach (Player player in gameData.players)
			{
				if (player.isHuman || player.isBarbarians) {
					continue;
				}
				//Do things with units.  Copy into an array first to avoid collection-was-modified exception
				foreach (MapUnit unit in player.units.ToArray())
				{
					if (unit.currentAIBehavior == null) {
						SetAIForUnit(unit);
					}
					
					//Now actually take actions
					//TODO: Move these into an AI method
					if (unit.currentAIBehavior is SettlerAI settlerAi) {
						if (unit.location == settlerAi.destination) {
							Console.WriteLine("Building city with " + unit);
							CityInteractions.BuildCity(unit.location.xCoordinate, unit.location.yCoordinate, player.guid, "Neo-Tokyo");
							UnitInteractions.disbandUnit(unit.guid);
						}
						else {
							//TODO: Move towards the destination
							Console.WriteLine("Moving settler unit towards " + settlerAi.destination);
						}
					}
					else if (unit.currentAIBehavior is DefenderAI defenderAI) {
						if (defenderAI.destination == unit.location) {
							UnitInteractions.fortifyUnit(unit.guid);
							Console.WriteLine("Fortifying " + unit + " at " + defenderAI.destination);
						}
						else {
							//TODO: Move towards destination
							Console.WriteLine("Moving defender towards " + defenderAI.destination);
						}
					}
					else if (unit.currentAIBehavior is ExplorerAI explorerAi) {
						Console.Write("Moving explorer AI for " + unit);
						//TODO: Distinguish between types of exploration
						//TODO: Make sure ON_A_BOAT units stay on the boat
						//Move randomly
						Tile newLocation = unit.unitType is SeaUnit ? unit.location.RandomCoastNeighbor() : unit.location.RandomLandNeighbor();
						//Because it chooses a semi-cardinal direction at random, not accounting for map, it could get none
						//if it tries to move e.g. north from the north pole.  Hence, this check.
						if (newLocation != Tile.NONE) {
							Console.WriteLine("Moving unit at " + unit.location + " to " + newLocation);
							unit.location.unitsOnTile.Remove(unit);
							newLocation.unitsOnTile.Add(unit);
							unit.location = newLocation;
						}
					}
				}
			}

            //City Production
            foreach (City city in gameData.cities)
            {
                IProducable producedItem = city.ComputeTurnProduction();
                if (producedItem != null) {
					if (producedItem is UnitPrototype prototype) {
						MapUnit newUnit = prototype.GetInstance();
						newUnit.owner = city.owner;
						newUnit.location = city.location;
						newUnit.facingDirection = TileDirection.SOUTHWEST;

						city.location.unitsOnTile.Add(newUnit);
						gameData.mapUnits.Add(newUnit);
						city.owner.AddUnit(newUnit);
	                }
					
					city.SetItemBeingProduced(CityProductionAI.GetNextItemToBeProduced(city, producedItem));
                }
            }
            //Reset movement points available for all units
            foreach (MapUnit mapUnit in gameData.mapUnits)
            {
                mapUnit.movementPointsRemaining = mapUnit.unitType.movement;
            }
            //Clear all wait queue, so if a player ended the turn without handling all waited units, they are selected
            //at the same place in the order.  Confirmed this is what Civ3 does.
            UnitInteractions.ClearWaitQueue();
            gameData.turn++;
        }
        private static void SetAIForUnit(MapUnit unit)
        {

	        //figure out an AI behavior
	        //TODO: Use strategies, not names
	        if (unit.unitType.name == "Settler") {
		        SettlerAI settlerAI = new SettlerAI();
		        settlerAI.goal = SettlerAI.SettlerGoal.BUILD_CITY;
		        //TODO: Better way of finding a destination
		        //TODO: Find a destination if there's already a city here.
		        settlerAI.destination = unit.location;
		        Console.WriteLine("Set AI for unit to settler AI with destination of " + settlerAI.destination);
		        unit.currentAIBehavior = settlerAI;
	        }
	        else if (unit.location.cityAtTile != null && unit.location.unitsOnTile.Count == 0) {
		        DefenderAI ai = new DefenderAI();
		        ai.goal = DefenderAI.DefenderGoal.DEFEND_CITY;
		        ai.destination = unit.location;
		        Console.WriteLine("Set defender AI for " + unit + " with destination of " + ai.destination);
		        unit.currentAIBehavior = ai;
	        }
	        else {
		        ExplorerAI ai = new ExplorerAI();
		        if (unit.unitType is SeaUnit) {
			        ai.type = ExplorerAI.ExplorationType.COASTLINE;
			        Console.WriteLine("Set coastline exploration AI for " + unit);
		        }
		        else if (unit.location.unitsOnTile.Exists((x) => x.unitType is SeaUnit)) {
			        ai.type = ExplorerAI.ExplorationType.ON_A_BOAT;
			        //TODO: Actually put the unit on the boat
			        Console.WriteLine("Set ON_A_BOAT exploration AI for " + unit);
		        }
		        else {
			        ai.type = ExplorerAI.ExplorationType.RANDOM;
			        Console.WriteLine("Set random exploration AI for " + unit);
		        }
		        unit.currentAIBehavior = ai;
	        }
        }

        ///Eventually we'll have a game year or month or whatever, but for now this provides feedback on our progression
        public static int GetTurnNumber()
        {
            return EngineStorage.gameData.turn;
        }
    }
}