using System;
using System.Collections.Generic;
using System.Linq;
using C7Engine.Pathing;
using C7GameData;
using C7GameData.AIData;
using C7Engine.AI;
using C7Engine.AI.StrategicAI;
using Serilog;

namespace C7Engine
{
	public class PlayerAI {
		private static ILogger log = Log.ForContext<PlayerAI>();

		public static void PlayTurn(Player player, Random rng)
		{
			if (player.isHuman || player.isBarbarians) {
				return;
			}
			log.Information("-> Begin " + player.civilization.cityNames[0] + " turn");

			if (player.turnsUntilPriorityReevaluation == 0) {
				log.Information("Re-evaluating strategic priorities for " + player);
				List<StrategicPriority> priorities = StrategicPriorityArbitrator.Arbitrate(player);
				player.strategicPriorityData.Clear();
				foreach (StrategicPriority priority in priorities) {
					player.strategicPriorityData.Add(priority);
				}
				player.turnsUntilPriorityReevaluation = 15 + GameData.rng.Next(10);
				log.Information(player.turnsUntilPriorityReevaluation + " turns until next re-evaluation");
			} else {
				player.turnsUntilPriorityReevaluation--;
			}

			//Do things with units.  Copy into an array first to avoid collection-was-modified exception
			foreach (MapUnit unit in player.units.ToArray()) {
				//For each unit, if there's already an AI task assigned, it will attempt to complete its goal.
				//It may fail due to conditions having changed since that goal was assigned; in that case it will
				//get a new task to try to complete.

				bool unitDone = false;
				int attempts = 0;
				int maxAttempts = 2;	//safety valve so we don't freeze the UI if SetAIForUnit returns something that fails
				while (!unitDone) {
					if (unit.currentAIData == null || attempts > 0) {
						SetAIForUnit(unit, player);
					}

					UnitAI artificialIntelligence = getAIForUnitStrategy(unit.currentAIData);
					unitDone = artificialIntelligence.PlayTurn(player, unit);

					attempts++;
					if (!unitDone && attempts >= maxAttempts) {
						log.Warning($"Hit max AI attempts of {maxAttempts} for unit {unit} at {unit.location} without succeeding.  This indicates SetAIForUnit returned an impossible task, and should be debugged.");
						break;
					}
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
					log.Information("No cities yet!  Set AI for unit to settler AI with destination of " + settlerAiData.destination);
				}
				else {
					settlerAiData.destination = SettlerLocationAI.findSettlerLocation(unit.location, player);
					if (settlerAiData.destination == Tile.NONE) {
						//This is possible if all tiles within 4 tiles of a city are either not land, or already claimed
						//by another colonist.  Longer-term, the AI shouldn't be building settlers if that is the case,
						//but right now we'll just spike the football to stop the clock and avoid building immediately next to another city.
						settlerAiData.goal = SettlerAIData.SettlerGoal.JOIN_CITY;
						log.Information("Set AI for unit to JOIN_CITY due to lack of locations to settle");
					}
					else {
						PathingAlgorithm algorithm = PathingAlgorithmChooser.GetAlgorithm();
						settlerAiData.pathToDestination = algorithm.PathFrom(unit.location, settlerAiData.destination);
						log.Information("Set AI for unit to BUILD_CITY with destination of " + settlerAiData.destination);
					}
				}
				unit.currentAIData = settlerAiData;
			}
			else if (unit.location.cityAtTile != null && unit.location.unitsOnTile.Count(u => u.unitType.defense > 0 && u != unit) == 0) {
				DefenderAIData ai = new DefenderAIData();
				ai.goal = DefenderAIData.DefenderGoal.DEFEND_CITY;
				ai.destination = unit.location;
				log.Information("Set defender AI for " + unit + " with destination of " + ai.destination);
				unit.currentAIData = ai;
			}
			else if (unit.unitType.name == "Catapult") {
				//For now tell catapults to sit tight.  It's getting really annoying watching them pointlessly bombard barb camps forever
				DefenderAIData ai = new DefenderAIData();
				ai.goal = DefenderAIData.DefenderGoal.DEFEND_CITY;
				ai.destination = unit.location;
				log.Information("Set defender AI for " + unit + " with destination of " + ai.destination);
				unit.currentAIData = ai;
			}
			else {

				if (unit.unitType.categories.Contains("Sea")) {
					ExplorerAIData ai = new ExplorerAIData();
					ai.type = ExplorerAIData.ExplorationType.COASTLINE;
					unit.currentAIData = ai;
					log.Information("Set coastline exploration AI for " + unit);
				}
				else if (unit.location.unitsOnTile.Exists((x) => x.unitType.categories.Contains("Sea"))) {
					ExplorerAIData ai = new ExplorerAIData();
					ai.type = ExplorerAIData.ExplorationType.ON_A_BOAT;
					unit.currentAIData = ai;
					//TODO: Actually put the unit on the boat
					log.Information("Set ON_A_BOAT exploration AI for " + unit);
				}
				else {
					//Isn't a Settler.  If there's a city at the location, it's defended.  No boats involved.  What's our priority?
					//If there is land to explore, we'll try to explore it.
					//Long-term TODO: Should only send tiles on this landmass.
					KeyValuePair<Tile, float> tileToExplore = ExplorerAI.FindTopScoringTileForExploration(player, player.tileKnowledge.AllKnownTiles().Where(t => t.IsLand()), ExplorerAIData.ExplorationType.RANDOM);
					if (tileToExplore.Value > 0) {
						ExplorerAIData ai = new ExplorerAIData();
						//What type of exploration should we do?
						int nearbyExplorers = 0;
						foreach (MapUnit mapUnit in player.units)
						{
							if (mapUnit.currentAIData is ExplorerAIData explorerAI) {
								if (explorerAI.type == ExplorerAIData.ExplorationType.NEAR_CITIES) {
									nearbyExplorers++;
								}
							}
						}
						if (nearbyExplorers < (player.cities.Count + 1)) {
							ai.type = ExplorerAIData.ExplorationType.NEAR_CITIES;
						} else {
							ai.type = ExplorerAIData.ExplorationType.RANDOM;
						}
						unit.currentAIData = ai;
						log.Information($"Set {ai.type} exploration AI for {unit}");
					}
					else {
						//Nowhere to explore.  What to do now?
						//Priority 1: Adequate defense of cities.
						//Future Priority 1: Escorting Settlers
						//Priority 2: Clearing out barbs
						//Priority 3: Defending chokepoints
						//Priority 4: ???
						//Priority 5: Profit!
						//(Realistically, as we evolve there will be a lot of options, such as defending borders from barbs, preparing attackers on other civs, defending
						//resources.  I expect we'll have some sort of arbiter that decides between competing priorities, with each being given a score as to how important
						//they are, including a weight by how far away the task is.  But this will evolve gradually over a long time)

						//As of today (4/7/2022), let's tackle just one of those - adequate defense of cities.  The AI is really good at losing cities to barbs right now,
						//and that's a problem.

						City nearestCityToDefend = FindNearbyCityToDefend(unit, player);

						DefenderAIData newUnitAIData = new DefenderAIData();
						newUnitAIData.destination = nearestCityToDefend.location;
						newUnitAIData.goal = DefenderAIData.DefenderGoal.DEFEND_CITY;

						PathingAlgorithm algorithm = PathingAlgorithmChooser.GetAlgorithm();
						newUnitAIData.pathToDestination = algorithm.PathFrom(unit.location, newUnitAIData.destination);

						log.Information($"Unit {unit} tasked with defending {nearestCityToDefend.name}");
						unit.currentAIData = newUnitAIData;
					}
				}
			}
		}

		/**
		 * Finds a nearby city that could use extra defenders.  Currently, that is a city that is tied
		 * for the fewest units present, and among those, it's the closest.
		 *
		 * This is not a brilliant method, with many flaws such as not considering units already en route to defend,
		 * whether the city needs more defenders, or if the units present are defenders.
		 *
		 * However, in the spirit of incrementalism, sending units to defend is still better than not sending them to defend.
		 */
		private static City FindNearbyCityToDefend(MapUnit unit, Player player)
		{
			int minDefenders = int.MaxValue;
			//TODO: Just being there doesn't mean a unit is a defender.
			List<City> citiesWithFewestDefenders = new List<City>();
			foreach (City c in player.cities) {
				if (c.location.unitsOnTile.Count < minDefenders) {
					minDefenders = c.location.unitsOnTile.Count;
					citiesWithFewestDefenders.Clear();
					citiesWithFewestDefenders.Add(c);
				}
			}
			City nearestCityToDefend = City.NONE;
			int closestCityDistance = int.MaxValue;
			foreach (City c in citiesWithFewestDefenders) {
				int distanceToCity = c.location.distanceTo(unit.location);
				if (distanceToCity < closestCityDistance) {
					nearestCityToDefend = c;
					closestCityDistance = distanceToCity;
				}
			}
			return nearestCityToDefend;
		}

		/**
		 * Medium-term solution to the problem of getting instances of the AI classes for polymorphic
		 * methods.
		 *
		 * Only the data will be stored on save, so only the data is guaranteed to be on the unit.
		 * There are several options - attach an instance whenever we need one, for example.
		 * But the AI implementations should be singletons that behave differently based on the
		 * data (and perhaps probability), so multiple instances doesn't really make sense.
		 *
		 * I fully expect this to evolve; at some point it might grab a Lua AI instead of a C# one
		 * too, for example, and one AIData class might be able to call up multiple types of AIs.
		 * It also likely will become mod-supporting someday, but we can't add everything on day one.
		 **/
		public static UnitAI getAIForUnitStrategy(UnitAIData aiData)
		{
			if (aiData is SettlerAIData sai) {
				return new SettlerAI();
			}
			else if (aiData is DefenderAIData dai) {
				return new DefenderAI();
			}
			else if (aiData is ExplorerAIData eai) {
				return new ExplorerAI();
			}
			throw new Exception("AI data not recognized");
		}
	}
}
