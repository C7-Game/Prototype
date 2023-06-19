namespace C7Engine
{
	using System;
	using System.Linq;
	using C7GameData;
	using C7GameData.Save;

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

			SaveGame save = SaveGame.Load(loadFilePath);
			GameData gameData = save.ToGameData();

			EngineStorage.gameData = gameData;

			Player humanPlayer = gameData.players.Any(p => p.isHuman) switch {
				true => gameData.players.Find(p => p.isHuman),
				false => gameData.CreateDummyGameData(),
			};
			Console.WriteLine($"human player: {humanPlayer.civilization.name}");

			EngineStorage.uiControllerID = humanPlayer.id;
			TurnHandling.OnBeginTurn(); // Call for the first turn
			TurnHandling.AdvanceTurn();

			EngineStorage.gameDataMutex.ReleaseMutex();
			return humanPlayer;
		}
	}
}
