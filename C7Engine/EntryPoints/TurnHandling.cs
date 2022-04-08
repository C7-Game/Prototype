using C7Engine.AI;

namespace C7Engine
{
	using C7GameData;
	using System;
	public class TurnHandling
	{
		internal static void OnBeginTurn()
		{
			GameData gameData = EngineStorage.gameData;
			Console.WriteLine("\n*** Beginning turn " + gameData.turn + " ***");

			//Reset movement points available for all units
			foreach (MapUnit mapUnit in gameData.mapUnits) {
				mapUnit.movementPointsRemaining = mapUnit.unitType.movement;
			}

			foreach (Player player in gameData.players) {
				player.hasPlayedThisTurn = false;
			}
		}

		// Implements the game loop. This method is called when the game is started and when the player signals that they're done moving.
		internal static void AdvanceTurn()
		{
			GameData gameData = EngineStorage.gameData;
			while (true) { // Loop ends with a function return once we reach the UI controller during the movement phase
				bool firstTurn = GetTurnNumber() == 0;

				// Movement phase
				foreach (Player player in gameData.players) {
					if ((! player.hasPlayedThisTurn) &&
					    ! (firstTurn && player.SitsOutFirstTurn())) {
						if (player.guid == EngineStorage.uiControllerID) {
							new MsgStartTurn().send();
							return;
						} else if (player.isBarbarians) {
							//Call the barbarian AI
							//TODO: The AIs should be stored somewhere on the game state as some of them will store state (plans,
							//strategy, etc.) For now, we only have a random AI, so that will be in a future commit
							new BarbarianAI().PlayTurn(player, gameData);
							player.hasPlayedThisTurn = true;
						} else if (! player.isHuman) {
							PlayerAI.PlayTurn(player, gameData.rng);
							player.hasPlayedThisTurn = true;
						} else {
							player.hasPlayedThisTurn = true;
						}
					}
				}

				//Clear all wait queue, so if a player ended the turn without handling all waited units, they are selected
				//at the same place in the order.  Confirmed this is what Civ3 does.
				UnitInteractions.ClearWaitQueue();

				// Production phase BEGIN

				Console.WriteLine("\n*** Processing production for turn " + gameData.turn + " ***");

				//Generate new barbarian units.
				foreach (Tile tile in gameData.map.barbarianCamps)
				{
					//7% chance of a new barbarian.  Probably should scale based on barbarian activity.
					int result = gameData.rng.Next(100);
					// Console.WriteLine("Random barb result = " + result);
					if (result < 7) {
						MapUnit newUnit = new MapUnit();
						newUnit.location = tile;
						newUnit.owner = gameData.players[0];
						newUnit.unitType = gameData.unitPrototypes["Warrior"];
						newUnit.hitPointsRemaining = 3;
						newUnit.isFortified = true; //todo: hack for unit selection

						tile.unitsOnTile.Add(newUnit);
						gameData.mapUnits.Add(newUnit);
						Console.WriteLine("New barbarian added at " + tile);
					}
					else if (tile.NeighborsWater() && result < 10) {
						MapUnit newUnit = new MapUnit();
						newUnit.location = tile;
						newUnit.owner = gameData.players[0];    //todo: make this reliably point to the barbs
						newUnit.unitType = gameData.unitPrototypes["Galley"];
						newUnit.hitPointsRemaining = 3;
						newUnit.isFortified = true; //todo: hack for unit selection

						tile.unitsOnTile.Add(newUnit);
						gameData.mapUnits.Add(newUnit);
						Console.WriteLine("New barbarian galley added at " + tile);
					}
				}

				//City Production
				foreach (City city in gameData.cities)
				{
					int initialSize = city.size;
					IProducible producedItem = city.ComputeTurnProduction();
					if (producedItem != null) {
						if (producedItem is UnitPrototype prototype) {
							MapUnit newUnit = prototype.GetInstance();
							newUnit.owner = city.owner;
							newUnit.location = city.location;
							newUnit.facingDirection = TileDirection.SOUTHWEST;

							city.location.unitsOnTile.Add(newUnit);
							gameData.mapUnits.Add(newUnit);
							city.owner.AddUnit(newUnit);
						}
						city.SetItemBeingProduced(CityProductionAI.GetNextItemToBeProduced(city, producedItem));
					}

					int newSize = city.size;
					if (newSize > initialSize) {
						CityResident newResident = new CityResident();
						newResident.nationality = city.owner.civilization;
						CityTileAssignmentAI.AssignNewCitizenToTile(city, newResident);
					}
					else if (newSize < initialSize) {
						int diff = initialSize - newSize;
						//Remove two residents.  Eventually, this will be prioritized by nationality, but for now just remove the last two
						for (int i = 1; i <= diff; i++) {
							city.residents[city.residents.Count - i].tileWorked.personWorkingTile = null;
							city.residents.RemoveAt(city.residents.Count - i);
						}
					}
				}

				// END Production phase

				gameData.turn++;
				OnBeginTurn();
			}
		}

		///Eventually we'll have a game year or month or whatever, but for now this provides feedback on our progression
		public static int GetTurnNumber()
		{
			return EngineStorage.gameData.turn;
		}
	}
}
