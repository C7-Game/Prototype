using System;
using System.Collections.Generic;
using System.Linq;
using C7Engine.Pathing;
using C7GameData;
using C7GameData.AIData;

namespace C7Engine
{
	public class SettlerAI
	{
		public static void PlaySettlerTurn(Player player, SettlerAIData settlerAi, MapUnit unit)
		{
			//The unit's destination may have become invalid due to a rival building a city there
			if (IsInvalidCityLocation(settlerAi.destination)) {
				Console.WriteLine("Seeking new destination for settler " + unit.guid + "headed to " + settlerAi.destination);
				PlayerAI.SetAIForUnit(unit, player);
				//Make sure we're using the new settler AI going forward, including this turn
				settlerAi = (SettlerAIData)unit.currentAIBehavior;
			}
			if (settlerAi.goal == SettlerAIData.SettlerGoal.JOIN_CITY && unit.location.cityAtTile != null) {
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
					return;
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
