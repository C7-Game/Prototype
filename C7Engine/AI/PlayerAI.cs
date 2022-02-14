using System;
using System.Collections.Generic;
using System.Linq;
using C7GameData;
using C7GameData.AIData;

namespace C7Engine
{
	public class PlayerAI
	{
		public static void PlayTurn(Player player, Random rng)
		{
			if (player.isHuman || player.isBarbarians) {
				return;
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
						CityInteractions.BuildCity(unit.location.xCoordinate, unit.location.yCoordinate, player.guid, unit.owner.GetNextCityName());
						UnitInteractions.disbandUnit(unit.guid);
					}
					else {
						MoveSettlerTowardsDestination(unit, settlerAi);
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
					List<Tile> possibleNewLocations = unit.unitType is SeaUnit ? unit.location.GetCoastNeighbors() : unit.location.GetLandNeighbors();
					Tile newLocation = possibleNewLocations[rng.Next(possibleNewLocations.Count)];
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
		
		/**
		 * Basic movement AI.  Ignores things such as roads, only works on land, ignores hazards such as barbarians.
		 * Which means it's not amazing yet, but it does get things moving in the right direction.
		 */
		private static void MoveSettlerTowardsDestination(MapUnit unit, SettlerAI settlerAi)
		{
			Dictionary<Tile, int> distances = new Dictionary<Tile, int>();
			foreach (Tile option in unit.location.GetLandNeighbors()) {
				distances[option] = settlerAi.destination.distanceToOtherTile(option);
			}
			IOrderedEnumerable<KeyValuePair<Tile, int>> orderedScores = distances.OrderBy(t => t.Value);
			Tile nextTile = null;
			foreach (KeyValuePair<Tile, int> kvp in orderedScores) {
				if (nextTile == null) {
					nextTile = kvp.Key;
				}
				Console.WriteLine("Settler could move to " + kvp.Key + " with distance value " + kvp.Value);
			}
			Console.WriteLine("Settler unit moving from " + unit.location + " to " + nextTile + " towards " + settlerAi.destination);
			unit.location.unitsOnTile.Remove(unit);
			nextTile.unitsOnTile.Add(unit);
			unit.location = nextTile;
		}

		private static void SetAIForUnit(MapUnit unit) 
		{
			//figure out an AI behavior
			//TODO: Use strategies, not names
			if (unit.unitType.name == "Settler") {
				SettlerAI settlerAI = new SettlerAI();
				settlerAI.goal = SettlerAI.SettlerGoal.BUILD_CITY;
				//If it's the starting settler, have it settle in place.  Otherwise, use an AI to find a location.
				if (unit.location.cityAtTile == null) {
					settlerAI.destination = unit.location;
				}
				else {
					settlerAI.destination = SettlerLocationAI.findSettlerLocation(unit.location);
				}
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
	}
}
