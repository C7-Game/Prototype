namespace C7Engine {
    using C7GameData;
    using System;

    public class BarbarianAI {
        public void PlayTurn(Player player, GameData gameData) {
            if (!player.isBarbarians) {
                throw new System.Exception("Barbarian AI can only play barbarian players");
            }

            foreach(MapUnit unit in gameData.mapUnits) {
                if (unit.owner == gameData.players[1]) {
                    if (unit.location.unitsOnTile.Count > 1 || unit.location.hasBarbarianCamp == false) {
                        //Move randomly
                        TileDirection randDir = Tile.RandomDirection();
                        Tile newLocation = unit.location.neighbors[randDir];
                        //Because it chooses a semi-cardinal direction at random, not accounting for map, it could get none
                        //if it tries to move e.g. north from the north pole.  Hence, this check.
                        //Longer term, we should enhance the code to only return valid destinations (which also means not water, etc.)
                        if (newLocation != Tile.NONE) {
                            Console.WriteLine("Moving barbarian at " + unit.location + " to " + newLocation);
                            unit.move(randDir);
                        }
                    }
                }
            }
        }
    }
}
