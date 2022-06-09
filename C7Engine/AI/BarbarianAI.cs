using System.Collections.Generic;
using System.IO;

namespace C7Engine {
	using C7GameData;
	using System;

	public class BarbarianAI {
		public void PlayTurn(Player player, GameData gameData) {
			if (!player.isBarbarians) {
				throw new System.Exception("Barbarian AI can only play barbarian players");
			}

			// Copy unit list into temporary array so we can remove units while iterating.
			// TODO: We also need to handle units spawned during the loop, e.g. leaders, armies, enslaved units. This is not so much an
			// issue for the barbs but will be for similar loops elsewhere in the AI logic.
			foreach(MapUnit unit in gameData.mapUnits.ToArray()) {
				//TODO: Make it better fit the barbs and not be hard-coded to a magic number
				if (unit.owner == gameData.players[0]) {
					if (unit.location.unitsOnTile.Count > 1 || unit.location.hasBarbarianCamp == false) {
						//Move randomly
						List<Tile> validTiles = unit.unitType.categories.Contains("Sea") ? unit.location.GetCoastNeighbors() : unit.location.GetLandNeighbors();
						if (validTiles.Count == 0) {
							//This can happen if a barbarian galley spawns next to a 1-tile lake, moves there, and doesn't have anywhere else to go.
							Console.WriteLine("WARNING: No valid tiles for barbarian to move to");
							continue;
						}
						Tile newLocation = validTiles[gameData.rng.Next(validTiles.Count)];
						//Because it chooses a semi-cardinal direction at random, not accounting for map, it could get none
						//if it tries to move e.g. north from the north pole.  Hence, this check.
						if (newLocation != Tile.NONE) {
							//This should be a low-level log
							// Console.WriteLine("Moving barbarian at " + unit.location + " to " + newLocation);
							unit.move(unit.location.directionTo(newLocation));
						}
					}
				}
			}
		}
	}
}
