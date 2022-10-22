using System.Linq;

namespace C7Engine
{
	using System;
	using C7GameData;

	public class CreateGame
	{
		/**
		 * For now, I'm making the methods that the C7 client can call be static.
		 * We may want a different solution in the end, but this lets us start prototyping
		 * quickly. By keeping all the client-callable APIs in the EntryPoints folder,
		 * hopefully it won't be too much of a goose hunt to refactor it later if we decide to do so.
		 **/
		public static Player createGame(string loadFilePath, string defaultBicPath)
		{
			EngineStorage.createThread();
			EngineStorage.gameDataMutex.WaitOne();

			C7SaveFormat save = SaveManager.Load(loadFilePath, defaultBicPath);

			Player humanPlayer = null;
			if (save.GameData.players.Count == 0) {
				Console.WriteLine("creating dummy data...");
				humanPlayer = save.GameData.CreateDummyGameData();
			} else {
				Console.WriteLine("loading player from save data...");
				humanPlayer = save.GameData.players.Find(p => p.isHuman);
			}

			EngineStorage.gameData = save.GameData;

			if (humanPlayer != null)
				EngineStorage.uiControllerID = humanPlayer.guid;
			else {
				//This occurs when a game is saved in Observer Mode
				//We still need this to be set for things like making sure the game doesn't autoplay forever in Observer Mode,
				//and for now can assume the first player is the human
				EngineStorage.uiControllerID = save.GameData.players.First().guid;
			}
			TurnHandling.OnBeginTurn(); // Call for the first turn
			TurnHandling.AdvanceTurn();

			EngineStorage.gameDataMutex.ReleaseMutex();
			return humanPlayer;
		}
	}
}
