using System.Collections.Generic;
using System.IO;
using System.Linq;
using Serilog;

namespace C7Engine {
	using C7GameData;
	using System;

	public class BarbarianAI {

		private ILogger log = Log.ForContext<BarbarianAI>();

		public void PlayTurn(Player player, GameData gameData) {
			if (!player.isBarbarians) {
				throw new System.Exception("Barbarian AI can only play barbarian players");
			}

			// Copy unit list into temporary array so we can remove units while iterating.
			// TODO: We also need to handle units spawned during the loop, e.g. leaders, armies, enslaved units. This is not so much an
			// issue for the barbs but will be for similar loops elsewhere in the AI logic.
			foreach (MapUnit unit in player.units.ToArray()) {
				if (UnitIsFreeToMove(unit)) {
					while (unit.movementPoints.canMove) {
						//Move randomly
						List<Tile> validTiles = unit.unitType.categories.Contains("Sea") ? unit.location.GetCoastNeighbors() : unit.location.GetLandNeighbors();
						if (validTiles.Count == 0) {
							//This can happen if a barbarian galley spawns next to a 1-tile lake, moves there, and doesn't have anywhere else to go.
							log.Warning("WARNING: No valid tiles for barbarian to move to");
							break;
						}
						Tile newLocation = validTiles[GameData.rng.Next(validTiles.Count)];
						//Because it chooses a semi-cardinal direction at random, not accounting for map, it could get none
						//if it tries to move e.g. north from the north pole.  Hence, this check.
						if (newLocation != Tile.NONE) {
							log.Debug("Moving barbarian at " + unit.location + " to " + newLocation);
							if (!unit.move(unit.location.directionTo(newLocation))) {
								break;
							}
						} else {
							//Avoid potential infinite loop.
							break;
						}
					}
				}
			}
		}
		private static bool UnitIsFreeToMove(MapUnit unit) {
			if (!unit.location.hasBarbarianCamp) {
				return true;
			}
			//If we're on a barb camp, only move if there's another unit defending
			return unit.location.unitsOnTile.Exists(mapUnit => mapUnit != unit && UnitIsLandDefender(mapUnit));
		}

		private static bool UnitIsLandDefender(MapUnit unit) {
			return unit.unitType.categories.Contains("Land") && unit.unitType.defense > 0;
		}
	}
}
