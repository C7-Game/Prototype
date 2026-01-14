using System.Diagnostics;
using C7Engine.AI;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Linq;
using Serilog;

namespace C7Engine {
	using System;
	using System.Threading.Tasks;
	using C7GameData;

	public class TurnHandling {
		private static ILogger log = Log.ForContext<TurnHandling>();

		internal static void OnBeginTurn() {
			GameData gameData = EngineStorage.gameData;
			log.Information("\n*** Beginning turn " + gameData.turn + " ***");

			foreach (MapUnit mapUnit in gameData.mapUnits)
				mapUnit.OnBeginTurn();
		}

		// Implements the game loop. This method is called when the game is started and when the player signals that they're done moving.
		internal static async Task AdvanceTurn() {
			Stopwatch stopwatch = new Stopwatch();
			stopwatch.Start();
			GameData gameData = EngineStorage.gameData;
			while (true) { // Loop ends with a function return once we reach the UI controller during the movement phase
				bool firstTurn = GetTurnNumber() == 0;

				// Movement phase
				if (await PlayPlayerTurns(gameData, firstTurn)) {
					stopwatch.Stop();
					log.Information("Turn time took " + stopwatch.ElapsedMilliseconds + " milliseconds");
					return;
				}

				//Clear all wait queue, so if a player ended the turn without handling all waited units, they are selected
				//at the same place in the order.  Confirmed this is what Civ3 does.
				UnitInteractions.ClearWaitQueue();

				SpawnBarbarians(gameData);

				gameData.turn++;
				foreach (Player player in gameData.players) {
					player.MaybeSpawnBonusUnits(gameData);
					player.RecalculateCitizenMoods(gameData, goIntoDisorderIfUnhappy: true);
					player.DoCorruptionCalculations(gameData);

					// Do the financial updates before updating the cities, so
					// that a newly produced unit won't put a player over the
					// unit support cap and cause them to lose gold unexpectedly.
					player.DoPerTurnFinanceUpdates(gameData);
					player.DoPerTurnScienceUpdates(gameData);

					// Note that we do growth after calculating citizen moods,
					// to ensure that the player has a chance to deal with the
					// unhappiness of a new citizen during their turn.
					log.Information($"\n*** City growth/production for turn {gameData.turn}, player {player} ***");
					player.HandleCityUpdates(gameData);
				}

				// Now that the turn is ending, do all the bookkeeping for the
				// start of the next turn. We don't put the "hasPlayedThisTurn"
				// logic in OnBeginTurn because OnBeginTurn is called when a
				// save game is loaded, and that would erase the saved information
				// about which players have played.
				OnBeginTurn();
				foreach (Player player in gameData.players) {
					player.hasPlayedThisTurn = false;
				}
			}
		}

		/// <summary>
		/// Plays the turns for all the players in the game (including barbarians).
		/// </summary>
		/// <param name="gameData"></param>
		/// <param name="firstTurn"></param>
		/// <returns>true when it is time for the human to take control again</returns>
		private static async Task<bool> PlayPlayerTurns(GameData gameData, bool firstTurn) {
			foreach (Player player in gameData.players) {
				if (player.hasPlayedThisTurn || player.defeated) {
					continue;
				}

				if (firstTurn && player.SitsOutFirstTurn()) {
					continue;
				}

				if (player.isBarbarians) {
					//Call the barbarian AI
					//TODO: The AIs should be stored somewhere on the game state as some of them will store state (plans,
					//strategy, etc.) For now, we only have a random AI, so that will be in a future commit
					await new BarbarianAI().PlayTurn(player, gameData);
					player.hasPlayedThisTurn = true;
				} else if (!player.isHuman) {
					await PlayerAI.PlayTurn(player, GameData.rng, gameData.techs);
					player.hasPlayedThisTurn = true;
				} else if (player.id != EngineStorage.uiControllerID) {
					player.hasPlayedThisTurn = true;
				}
				//Human player check.  Let the human see what's going on even if they are in observer mode.
				if (player.id == EngineStorage.uiControllerID) {
					new MsgStartTurn().send();
					return true;
				}
			}
			return false;
		}

		private static void SpawnBarbarians(GameData gameData) {
			Player barbPlayer = gameData.players.Find(player => player.isBarbarians);

			// A random 5% of camps will spawn a unit each turn. Shuffle the
			// camps to make this random.
			int barbariansToSpawn = (int)Math.Ceiling(GameData.rng.Next(gameData.map.barbarianCamps.Count) / 20.0);
			List<int> tileIndicies = Enumerable.Range(0, gameData.map.barbarianCamps.Count).ToList();
			GameData.rng.Shuffle<int>(CollectionsMarshal.AsSpan(tileIndicies));

			for (int i = 0; i < barbariansToSpawn; ++i) {
				Tile tile = gameData.map.barbarianCamps[tileIndicies[i]];
				MapUnit newUnit = new(gameData.ids.CreateID("barbarian"));
				newUnit.location = tile;
				newUnit.owner = barbPlayer;
				// TODO: make this a conscript.
				newUnit.experienceLevelKey = gameData.defaultExperienceLevelKey;
				newUnit.experienceLevel = gameData.defaultExperienceLevel;
				newUnit.hitPointsRemaining = 3;
				tile.unitsOnTile.Add(newUnit);
				gameData.mapUnits.Add(newUnit);
				barbPlayer.units.Add(newUnit);

				// Give costal camps a chance to spawn a boat.
				if (tile.NeighborsWater() && GameData.rng.Next(100) < 20) {
					newUnit.unitType = gameData.barbarianInfo.barbarianSeaUnitProto;
					log.Debug("New barbarian galley added at " + tile);
					continue;
				}

				// Otherwise its a 3:1 ratio of advanced to basic barbarians.
				newUnit.unitType = GameData.rng.Next(100) < 25 ? gameData.barbarianInfo.advancedBarbarian : gameData.barbarianInfo.basicBarbarian;
				log.Debug("New barbarian added at " + tile);
			}
		}

		///Eventually we'll have a game year or month or whatever, but for now this provides feedback on our progression
		public static int GetTurnNumber() {
			return EngineStorage.gameData.turn;
		}
	}
}
