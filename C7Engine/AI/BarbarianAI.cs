namespace C7Engine {
	using C7GameData;
	using System;

	public class BarbarianAI {
		public void PlayTurn(Player player, GameData gameData) {
			if (!player.isBarbarians) {
				throw new System.Exception("Barbarian AI can only play barbarian players");
			}

			// Iterate by index & backwards so that we can remove items from the mapUnits list inside this for loop w/o crashing the
			// program. This can happen when unit movement triggers combat which kills the unit. TODO: This is hopefully a temporary
			// thing. I think a better long term solution is to do a foreach loop but keep a list of units to be deleted after the loop is
			// finished. Problem is that has to work for combat triggered during the player's turn as well. Also right now we have zero HP
			// units running around that we wouldn't want to delete.
			for (int n = gameData.mapUnits.Count - 1; n >= 0; n--) {
				MapUnit unit = gameData.mapUnits[n];
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
