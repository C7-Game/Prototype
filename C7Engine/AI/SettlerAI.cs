using System;
using C7Engine.Pathing;
using C7GameData;
using C7GameData.AIData;

namespace C7Engine
{
	public class SettlerAI
	{
		public static void PlaySettlerTurn(Player player, SettlerAIData settlerAi, MapUnit unit)
		{
start:
			switch (settlerAi.goal) {
				case SettlerAIData.SettlerGoal.BUILD_CITY:
					if (IsInvalidCityLocation(settlerAi.destination)) {
						Console.WriteLine("Seeking new destination for settler " + unit.guid + "headed to " + settlerAi.destination);
						PlayerAI.SetAIForUnit(unit, player);
						//Make sure we're using the new settler AI going forward, including this turn
						settlerAi = (SettlerAIData)unit.currentAIBehavior;
						//Re-process since the unit's goal may have changed.
						//TODO: In theory in the future, it might even have a non-settler AI.  Maybe we should instead return false,
						//and have the PlayerAI re-kick the unit based on a possibly different AI class?
						//Not too worried for settler AI types, but that's a real possibility for other types - an Explorer could
						//very well become a Defender or Attacker if there's no exploration left, for example.
						goto start;
					}
					if (unit.location == settlerAi.destination) {
						Console.WriteLine("Building city with " + unit);
						CityInteractions.BuildCity(unit.location.xCoordinate, unit.location.yCoordinate, player.guid, unit.owner.GetNextCityName());
						UnitInteractions.disbandUnit(unit.guid);
					}
					else {
						//If the settler has no destination, then disband rather than crash later.
						if (settlerAi.destination == Tile.NONE) {
							Console.WriteLine("Disbanding settler " + unit.guid + " with no valid destination");
							UnitInteractions.disbandUnit(unit.guid);
							return;
						}
						if (settlerAi.pathToDestination == null) {
							PathingAlgorithm algorithm = PathingAlgorithmChooser.GetAlgorithm();
							settlerAi.pathToDestination = algorithm.PathFrom(unit.location, settlerAi.destination);
						}
						try {
							Tile nextTile = settlerAi.pathToDestination.Next();
							unit.location.unitsOnTile.Remove(unit);
							nextTile.unitsOnTile.Add(unit);
							unit.location = nextTile;
						}
						catch(Exception ex) {
							Console.WriteLine("Could not get next part of path for unit " + settlerAi);
						}
					}
					break;
				case SettlerAIData.SettlerGoal.JOIN_CITY:
					if (unit.location.cityAtTile != null) {
						//TODO: Actually join the city.  Haven't added that action.
						//For now, just get rid of the unit.  Sorry, bro.
						UnitInteractions.disbandUnit(unit.guid);
					}
					else {
						//TODO: Eventually, go to the city we're supposed to join
						//For now, just disband
						UnitInteractions.disbandUnit(unit.guid);
					}
					break;
				default:
					Console.WriteLine("Unknown strategy of " + settlerAi.goal + " for unit");
					break;
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
