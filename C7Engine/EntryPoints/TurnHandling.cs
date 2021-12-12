namespace C7Engine
{
    using C7GameData;
    using System;
    public class TurnHandling
    {
        public static void EndTurn()
        {
            GameData gameData = EngineStorage.gameData;
            //Barbarians
            //We should really have a top-level list of tiles with barb camps
            foreach (Tile tile in gameData.map.tiles)
            {
                if (tile.hasBarbarianCamp) {
                    //7% chance of a new barbarian.  Probably should scale based on barbarian activity.  
                    Random rnd = new Random();
                    int result = rnd.Next(100);
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
                        newUnit.unitType = newUnitPrototype;
                        newUnit.isFortified = true; //todo: hack for unit selection

                        tile.unitsOnTile.Add(newUnit);
                        gameData.mapUnits.Add(newUnit);
                        Console.WriteLine("New barbarian added at " + tile);
                    }
                }
            }
            foreach(MapUnit unit in gameData.mapUnits) {
                if (unit.owner == gameData.players[1]) {
                    if (unit.location.unitsOnTile.Count > 1 || unit.location.hasBarbarianCamp == false) {
                        //Move randomly
                        Tile newLocation = unit.location.neighbors[Tile.RandomDirection()];
                        //Because it chooses a semi-cardinal direction at random, not accounting for map, it could get none
                        //if it tries to move e.g. north from the north pole.  Hence, this check.
                        //Longer term, we should enhance the code to only return valid destinations (which also means not water, etc.)
                        if (newLocation != Tile.NONE) {
                            Console.WriteLine("Moving barbarian at " + unit.location + " to " + newLocation);
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