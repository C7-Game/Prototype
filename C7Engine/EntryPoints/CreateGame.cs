namespace C7Engine
{
	using System;
	using C7GameData;

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

			Player humanPlayer = null;
			if (save.GameData.players.Count == 0) {
				Console.WriteLine("creating dummy data...");
				humanPlayer = save.GameData.CreateDummyGameData();
			} else {
				Console.WriteLine("loading player from save data...");
				humanPlayer = save.GameData.players.Find(p => p.isHuman);
			}
			if (humanPlayer == null) {
				Console.WriteLine("null player!");
			}
			foreach (MapUnit unit in humanPlayer.units) {
				Console.WriteLine(unit);
			}



			EngineStorage.gameData = save.GameData;
			Console.WriteLine("mapUnits length: " + EngineStorage.gameData.mapUnits.Count);
			foreach (MapUnit unit in save.GameData.mapUnits) {
				Console.WriteLine("Unit GUID: " + unit.guid);
			}
			Console.WriteLine("End unit iteration");


			EngineStorage.uiControllerID = humanPlayer.guid;
			TurnHandling.OnBeginTurn(); // Call for the first turn
			TurnHandling.AdvanceTurn();

			EngineStorage.gameDataMutex.ReleaseMutex();
			return humanPlayer;
		}
	}
}
