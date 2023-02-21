using C7GameData;
using System;

namespace C7Engine
{

	public class CreateGame
	{
		/**
		 * For now, I'm making the methods that the C7 client can call be static.
		 * We may want a different solution in the end, but this lets us start prototyping
		 * quickly.  By keeping all the client-callable APIs in the EntryPoints folder,
		 * hopefully it won't be too much of a goose hunt to refactor it later if we decide to do so.
		 **/
		public static Player createGame(string loadFilePath, string defaultBicPath)
		{
			EngineStorage.createThread();
			EngineStorage.gameDataMutex.WaitOne();

			C7SaveFormat save = SaveManager.Load(loadFilePath, defaultBicPath);
			EngineStorage.gameData = save.GameData;
			// Consider if we have any need to keep a reference to the save object handy...probably not

			// var humanPlayer = save.GameData.players.Find(p => p.isHuman);
			// Console.WriteLine("human player city 0: " +humanPlayer.civilization.cityNames[0]);
			// Console.WriteLine("human player guid: " + humanPlayer.guid.ToString());
			// save.GameData.turn = 0;

			var humanPlayer = save.GameData.CreateDummyGameData();
			EngineStorage.uiControllerID = humanPlayer.guid;
			TurnHandling.OnBeginTurn(); // Call for the first turn
			TurnHandling.AdvanceTurn();

			EngineStorage.gameDataMutex.ReleaseMutex();
			return humanPlayer;
		}
	}
}
