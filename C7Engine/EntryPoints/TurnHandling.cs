using System.Diagnostics;
using C7Engine.AI;
using Serilog;

namespace C7Engine
{
	using C7GameData;
	using System;
	public class TurnHandling {
		private static ILogger log = Log.ForContext<TurnHandling>();

		internal static void OnBeginTurn()
		{
			GameData gameData = EngineStorage.gameData;
			log.Information("\n*** Beginning turn " + gameData.turn + " ***");

			foreach (MapUnit mapUnit in gameData.mapUnits)
				mapUnit.OnBeginTurn();

			foreach (Player player in gameData.players) {
				player.hasPlayedThisTurn = false;
			}
		}

		// Implements the game loop. This method is called when the game is started and when the player signals that they're done moving.
		internal static void AdvanceTurn() {
			Stopwatch stopwatch = new Stopwatch();
			stopwatch.Start();
			GameData gameData = EngineStorage.gameData;
			while (true) { // Loop ends with a function return once we reach the UI controller during the movement phase
				bool firstTurn = GetTurnNumber() == 0;

				// Movement phase
				if (PlayPlayerTurns(gameData, firstTurn)) {
					stopwatch.Stop();
					log.Information("Turn time took " + stopwatch.ElapsedMilliseconds + " milliseconds");
					return;
				}

				//Clear all wait queue, so if a player ended the turn without handling all waited units, they are selected
				//at the same place in the order.  Confirmed this is what Civ3 does.
				UnitInteractions.ClearWaitQueue();

				SpawnBarbarians(gameData);
				HandleCityResults(gameData);

				gameData.turn++;
				OnBeginTurn();
			}
		}

		/// <summary>
		/// Plays the turns for all the players in the game (including barbarians).
		/// </summary>
		/// <param name="gameData"></param>
		/// <param name="firstTurn"></param>
		/// <returns>true when it is time for the human to take control again</returns>
		private static bool PlayPlayerTurns(GameData gameData, bool firstTurn)
		{
			foreach (Player player in gameData.players) {
				if ((!player.hasPlayedThisTurn) &&
				    !(firstTurn && player.SitsOutFirstTurn())) {
					if (player.isBarbarians) {
						//Call the barbarian AI
						//TODO: The AIs should be stored somewhere on the game state as some of them will store state (plans,
						//strategy, etc.) For now, we only have a random AI, so that will be in a future commit
						new BarbarianAI().PlayTurn(player, gameData);
						player.hasPlayedThisTurn = true;
					}
					else if (!player.isHuman) {
						PlayerAI.PlayTurn(player, GameData.rng);
						player.hasPlayedThisTurn = true;
					}
					else if (player.id != EngineStorage.uiControllerID) {
						player.hasPlayedThisTurn = true;
					}
					//Human player check.  Let the human see what's going on even if they are in observer mode.
					if (player.id == EngineStorage.uiControllerID) {
						new MsgStartTurn().send();
						return true;
					}
				}
			}
			return false;
		}
		private static void SpawnBarbarians(GameData gameData)
		{
			//Generate new barbarian units.
			Player barbPlayer = gameData.players.Find(player => player.isBarbarians);
			foreach (Tile tile in gameData.map.barbarianCamps) {
				//7% chance of a new barbarian.  Probably should scale based on barbarian activity.
				int result = GameData.rng.Next(100);
				log.Verbose("Random barb result = " + result);
				if (result < 4) {
					MapUnit newUnit = new MapUnit(gameData.ids.CreateID("barbarian"));
					newUnit.location = tile;
					newUnit.owner = gameData.players[0];
					newUnit.unitType = gameData.barbarianInfo.basicBarbarian;
					newUnit.experienceLevelKey = gameData.defaultExperienceLevelKey;
					newUnit.experienceLevel = gameData.defaultExperienceLevel;
					newUnit.hitPointsRemaining = 3;
					newUnit.isFortified = true; //todo: hack for unit selection

					tile.unitsOnTile.Add(newUnit);
					gameData.mapUnits.Add(newUnit);
					barbPlayer.units.Add(newUnit);
					log.Debug("New barbarian added at " + tile);
				}
				else if (tile.NeighborsWater() && result < 6) {
					MapUnit newUnit = new MapUnit(gameData.ids.CreateID(gameData.barbarianInfo.barbarianSeaUnit.name));
					newUnit.location = tile;
					newUnit.owner = gameData.players[0]; //todo: make this reliably point to the barbs
					newUnit.unitType = gameData.barbarianInfo.barbarianSeaUnit;
					newUnit.experienceLevelKey = gameData.defaultExperienceLevelKey;
					newUnit.experienceLevel = gameData.defaultExperienceLevel;
					newUnit.hitPointsRemaining = 3;
					newUnit.isFortified = true; //todo: hack for unit selection

					tile.unitsOnTile.Add(newUnit);
					gameData.mapUnits.Add(newUnit);
					barbPlayer.units.Add(newUnit);
					log.Debug("New barbarian galley added at " + tile);
				}
			}
		}
		private static void HandleCityResults(GameData gameData)
		{

			log.Information("\n*** City production for turn " + gameData.turn + " ***");

			foreach (City city in gameData.cities) {
				int initialSize = city.size;
				city.ComputeCityGrowth();
				int newSize = city.size;
				if (newSize > initialSize) {
					CityResident newResident = new CityResident();
					newResident.nationality = city.owner.civilization;
					CityTileAssignmentAI.AssignNewCitizenToTile(city, newResident);
				}
				else if (newSize < initialSize) {
					int diff = initialSize - newSize;
					if (newSize <= 0) {
						log.Error($"Attempting to remove the last resident from {city}");
					}
					else {
						city.RemoveCitizens(diff);
					}
				}

				IProducible producedItem = city.ComputeTurnProduction();
				if (producedItem != null) {
					log.Information($"Produced {producedItem} in {city}");
					if (producedItem is UnitPrototype prototype) {
						MapUnit newUnit = prototype.GetInstance(gameData);
						newUnit.owner = city.owner;
						newUnit.location = city.location;
						newUnit.experienceLevelKey = gameData.defaultExperienceLevelKey;
						newUnit.experienceLevel = gameData.defaultExperienceLevel;
						newUnit.facingDirection = TileDirection.SOUTHWEST;

						city.location.unitsOnTile.Add(newUnit);
						gameData.mapUnits.Add(newUnit);
						city.owner.AddUnit(newUnit);

						if (newUnit.unitType.populationCost > 0) {
							city.RemoveCitizens(newUnit.unitType.populationCost);
						}
					}
					city.SetItemBeingProduced(CityProductionAI.GetNextItemToBeProduced(city, producedItem));
				}
			}
		}

		///Eventually we'll have a game year or month or whatever, but for now this provides feedback on our progression
		public static int GetTurnNumber()
		{
			return EngineStorage.gameData.turn;
		}
	}
}
