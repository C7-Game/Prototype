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
				if (unit.currentAIData == null) {
					SetAIForUnit(unit, player);
				}
				
				//Now actually take actions
				//TODO: Move these into an AI method
				if (unit.currentAIData is SettlerAIData settlerAi) {
					SettlerAI.PlaySettlerTurn(player, settlerAi, unit);
				}
				else if (unit.currentAIData is DefenderAIData defenderAI) {
					if (defenderAI.destination == unit.location) {
						if (!unit.isFortified) {
							unit.fortify();
							Console.WriteLine("Fortifying " + unit + " at " + defenderAI.destination);
						}
					}
					else {
						//TODO: Move towards destination
						Console.WriteLine("Moving defender towards " + defenderAI.destination);
					}
				}
				else if (unit.currentAIData is ExplorerAIData explorerAi) {
					ExplorerAI.PlayExplorerTurn(player, explorerAi, unit);
				}
				player.tileKnowledge.AddTilesToKnown(unit.location);
			}
		}
		
		public static void SetAIForUnit(MapUnit unit, Player player) 
		{
			//figure out an AI behavior
			//TODO: Use strategies, not names
			if (unit.unitType.name == "Settler") {
				SettlerAIData settlerAiData = new SettlerAIData();
				settlerAiData.goal = SettlerAIData.SettlerGoal.BUILD_CITY;
				//If it's the starting settler, have it settle in place.  Otherwise, use an AI to find a location.
				if (player.cities.Count == 0 && unit.location.cityAtTile == null) {
					settlerAiData.destination = unit.location;
					Console.WriteLine("No cities yet!  Set AI for unit to settler AI with destination of " + settlerAiData.destination);
				}
				else {
					settlerAiData.destination = SettlerLocationAI.findSettlerLocation(unit.location, player.cities, player.units);
					if (settlerAiData.destination == Tile.NONE) {
						//This is possible if all tiles within 4 tiles of a city are either not land, or already claimed
						//by another colonist.  Longer-term, the AI shouldn't be building settlers if that is the case,
						//but right now we'll just spike the football to stop the clock and avoid building immediately next to another city.
						settlerAiData.goal = SettlerAIData.SettlerGoal.JOIN_CITY;
						Console.WriteLine("Set AI for unit to JOIN_CITY due to lack of locations to settle");
					}
					else {
						PathingAlgorithm algorithm = PathingAlgorithmChooser.GetAlgorithm();
						settlerAiData.pathToDestination = algorithm.PathFrom(unit.location, settlerAiData.destination);
						Console.WriteLine("Set AI for unit to BUILD_CITY with destination of " + settlerAiData.destination);
					}
				}
				unit.currentAIData = settlerAiData;
			}
			else if (unit.location.cityAtTile != null && unit.location.unitsOnTile.Count(u => u.unitType.defense > 0 && u != unit) == 0) {
				DefenderAIData ai = new DefenderAIData();
				ai.goal = DefenderAIData.DefenderGoal.DEFEND_CITY;
				ai.destination = unit.location;
				Console.WriteLine("Set defender AI for " + unit + " with destination of " + ai.destination);
				unit.currentAIData = ai;
			}
			else {
				ExplorerAIData ai = new ExplorerAIData();
				if (unit.unitType is SeaUnit) {
					ai.type = ExplorerAIData.ExplorationType.COASTLINE;
					Console.WriteLine("Set coastline exploration AI for " + unit);
				}
				else if (unit.location.unitsOnTile.Exists((x) => x.unitType is SeaUnit)) {
					ai.type = ExplorerAIData.ExplorationType.ON_A_BOAT;
					//TODO: Actually put the unit on the boat
					Console.WriteLine("Set ON_A_BOAT exploration AI for " + unit);
				}
				else {
					ai.type = ExplorerAIData.ExplorationType.RANDOM;
					Console.WriteLine("Set random exploration AI for " + unit);
				}
				unit.currentAIData = ai;
			}
		}
	}
}
