using System.Collections.Generic;
using System.Linq;
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
                int result = gameData.rng.Next(100);
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
                else if (tile.NeighborsWater() && result < 10) {
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
				PlayerAI.PlayTurn(player, gameData.rng);
			}

            //City Production
            foreach (City city in gameData.cities)
            {
                IProducible producedItem = city.ComputeTurnProduction();
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

        ///Eventually we'll have a game year or month or whatever, but for now this provides feedback on our progression
        public static int GetTurnNumber()
        {
            return EngineStorage.gameData.turn;
        }
    }
}