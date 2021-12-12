namespace C7Engine
{
    using C7GameData;
    public class TurnHandling
    {
        public static void EndTurn()
        {
            GameData gameData = EngineStorage.gameData;
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
        }

        ///Eventually we'll have a game year or month or whatever, but for now this provides feedback on our progression
        public static int GetTurnNumber()
        {
            return EngineStorage.gameData.turn;
        }
    }
}