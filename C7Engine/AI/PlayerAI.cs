using System;
using System.Collections.Generic;
using System.Linq;
using C7Engine.Pathing;
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
			Console.WriteLine("-> Begin " + player.civilization.cityNames[0] + " turn");
			//Do things with units.  Copy into an array first to avoid collection-was-modified exception
			foreach (MapUnit unit in player.units.ToArray())
			{
				if (unit.currentAIBehavior == null) {
					SetAIForUnit(unit, player);
				}
				
				//Now actually take actions
				//TODO: Move these into an AI method
				if (unit.currentAIBehavior is SettlerAI settlerAi) {
					//The unit's destination may have become invalid due to a rival building a city there
					if (IsInvalidCityLocation(settlerAi.destination)) {
						Console.WriteLine("Seeking new destination for settler " + unit.guid + "headed to " + settlerAi.destination);
						SetAIForUnit(unit, player);
						//Make sure we're using the new settler AI going forward, including this turn
						settlerAi = (SettlerAI)unit.currentAIBehavior;
					}
					if (settlerAi.goal == SettlerAI.SettlerGoal.JOIN_CITY && unit.location.cityAtTile != null) {
						//TODO: Actually join the city.  Haven't added that action.
						//For now, just get rid of the unit.  Sorry, bro.
						UnitInteractions.disbandUnit(unit.guid);
					}
					else if (unit.location == settlerAi.destination) {
						Console.WriteLine("Building city with " + unit);
						CityInteractions.BuildCity(unit.location.xCoordinate, unit.location.yCoordinate, player.guid, unit.owner.GetNextCityName());
						UnitInteractions.disbandUnit(unit.guid);
					}
					else {
						//If the settler has no destination, then disband rather than crash later.
						if (settlerAi.destination == Tile.NONE) {
							Console.WriteLine("Disbanding settler " + unit.guid + " with no valid destination");
							UnitInteractions.disbandUnit(unit.guid);
							continue;
						}
						if (settlerAi.pathToDestination == null) {
							PathingAlgorithm algorithm = PathingAlgorithmChooser.GetAlgorithm();
							settlerAi.pathToDestination = algorithm.PathFrom(unit.location, settlerAi.destination);
						}
						Tile nextTile = settlerAi.pathToDestination.Next();
						unit.location.unitsOnTile.Remove(unit);
						nextTile.unitsOnTile.Add(unit);
						unit.location = nextTile;
					}
				}
				else if (unit.currentAIBehavior is DefenderAI defenderAI) {
					if (defenderAI.destination == unit.location) {
						if (!unit.isFortified) {
							UnitInteractions.fortifyUnit(unit.guid);
							Console.WriteLine("Fortifying " + unit + " at " + defenderAI.destination);
						}
					}
					else {
						//TODO: Move towards destination
						Console.WriteLine("Moving defender towards " + defenderAI.destination);
					}
				}
				else if (unit.currentAIBehavior is ExplorerAI explorerAi) {
					// Console.Write("Moving explorer AI for " + unit);
					//TODO: Distinguish between types of exploration
					//TODO: Make sure ON_A_BOAT units stay on the boat
					//Move randomly
					List<Tile> possibleNewLocations = unit.unitType is SeaUnit ? unit.location.GetCoastNeighbors() : unit.location.GetLandNeighbors();
					Tile newLocation = possibleNewLocations[rng.Next(possibleNewLocations.Count)];
					//Because it chooses a semi-cardinal direction at random, not accounting for map, it could get none
					//if it tries to move e.g. north from the north pole.  Hence, this check.
					if (newLocation != Tile.NONE) {
						// Console.WriteLine("Moving unit at " + unit.location + " to " + newLocation);
						unit.location.unitsOnTile.Remove(unit);
						newLocation.unitsOnTile.Add(unit);
						unit.location = newLocation;
					}
				}
			}
		}

		private static void SetAIForUnit(MapUnit unit, Player player) 
		{
			//figure out an AI behavior
			//TODO: Use strategies, not names
			if (unit.unitType.name == "Settler") {
				SettlerAI settlerAI = new SettlerAI();
				settlerAI.goal = SettlerAI.SettlerGoal.BUILD_CITY;
				//If it's the starting settler, have it settle in place.  Otherwise, use an AI to find a location.
				if (player.cities.Count == 0 && unit.location.cityAtTile == null) {
					settlerAI.destination = unit.location;
					Console.WriteLine("No cities yet!  Set AI for unit to settler AI with destination of " + settlerAI.destination);
				}
				else {
					settlerAI.destination = SettlerLocationAI.findSettlerLocation(unit.location, player.cities, player.units);
					if (settlerAI.destination == Tile.NONE) {
						//This is possible if all tiles within 4 tiles of a city are either not land, or already claimed
						//by another colonist.  Longer-term, the AI shouldn't be building settlers if that is the case,
						//but right now we'll just spike the football to stop the clock and avoid building immediately next to another city.
						settlerAI.goal = SettlerAI.SettlerGoal.JOIN_CITY;
						Console.WriteLine("Set AI for unit to JOIN_CITY due to lack of locations to settle");
					}
					else {
						Console.WriteLine("Set AI for unit to BUILD_CITY with destination of " + settlerAI.destination);
					}
				}
				unit.currentAIBehavior = settlerAI;
			}
			else if (unit.location.cityAtTile != null && unit.location.unitsOnTile.Count(u => u.unitType.defense > 0 && u != unit) == 0) {
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

		private static bool IsInvalidCityLocation(Tile tile)
		{
			if (tile.cityAtTile != null) {
				Console.WriteLine("Cannot build at " + tile + " due to city of " + tile.cityAtTile.name);
				return true;
			}
			foreach (Tile neighbor in tile.neighbors.Values)
			{
				if (neighbor.cityAtTile != null) {
					Console.WriteLine("Cannot build at " + tile + " due to nearby city of " + neighbor.cityAtTile.name);
					return true;
				}
			}
			return false;
		}
	}
}
