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
                    newUnit.owner = gameData.players[1];    //todo: make this reliably point to the barbs
                    UnitPrototype newUnitPrototype = new UnitPrototype();
                    newUnitPrototype.name = "Warrior";
                    newUnitPrototype.attack = 1;
                    newUnitPrototype.defense = 1;
                    newUnitPrototype.movement = 1;
                    newUnitPrototype.iconIndex = 6;
                    newUnit.hitPointsRemaining = 3;
                    newUnit.unitType = newUnitPrototype;
                    newUnit.isFortified = true; //todo: hack for unit selection

                    tile.unitsOnTile.Add(newUnit);
                    gameData.mapUnits.Add(newUnit);
                    Console.WriteLine("New barbarian added at " + tile);
                }
                else if (tile.NeighborsCoast() && result < 10) {
                    MapUnit newUnit = new MapUnit();
                    newUnit.location = tile;
                    newUnit.owner = gameData.players[1];    //todo: make this reliably point to the barbs
                    SeaUnit newUnitPrototype = new SeaUnit();
                    newUnitPrototype.name = "Galley";
                    newUnitPrototype.attack = 1;
                    newUnitPrototype.defense = 1;
                    newUnitPrototype.movement = 3;
                    newUnitPrototype.iconIndex = 29;
                    newUnit.hitPointsRemaining = 3;
                    newUnit.unitType = newUnitPrototype;
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
            barbarianAI.PlayTurn(gameData.players[1], gameData);

            //City Production
            foreach (City city in gameData.cities)
            {
                string producedItem = city.ComputeTurnProduction();
                if (producedItem != "") {
                    MapUnit newUnit = new MapUnit();
                    //TODO: It's inconsistent that one of them stores Tile, the other stores X, Y coordinates
                    newUnit.location = gameData.map.tileAt(city.xLocation, city.yLocation);
                    newUnit.hitPointsRemaining = 3;
                    newUnit.owner = gameData.players[0];
                    //This should not be re-genned here
                    UnitPrototype newUnitPrototype = new UnitPrototype();

                    if (producedItem == "Warrior") {
                        newUnitPrototype.name = "Warrior";
                        newUnitPrototype.attack = 1;
                        newUnitPrototype.defense = 1;
                        newUnitPrototype.movement = 1;
                        newUnitPrototype.iconIndex = 6;
                        newUnit.unitType = newUnitPrototype;
                    }
                    else if (producedItem == "Chariot") {
                        newUnitPrototype.name = "Chariot";
                        newUnitPrototype.attack = 1;
                        newUnitPrototype.defense = 1;
                        newUnitPrototype.movement = 3;
                        newUnitPrototype.iconIndex = 10;
                        newUnit.unitType = newUnitPrototype;
                    }
                    else if (producedItem == "Settler") {
                        newUnitPrototype.name = "Settler";
                        newUnitPrototype.attack = 0;
                        newUnitPrototype.defense = 0;
                        newUnitPrototype.movement = 1;
                        newUnitPrototype.iconIndex = 0;
                        newUnit.unitType = newUnitPrototype;
                    }
                    newUnit.location.unitsOnTile.Add(newUnit);
                    gameData.mapUnits.Add(newUnit);
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

	    new MsgStartTurn().send();
        }

        ///Eventually we'll have a game year or month or whatever, but for now this provides feedback on our progression
        public static int GetTurnNumber()
        {
            return EngineStorage.gameData.turn;
        }
    }
}
