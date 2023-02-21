using C7GameData;

namespace C7Engine {

	public class CreateGame {
		/**
		 * For now, I'm making the methods that the C7 client can call be static.
		 * We may want a different solution in the end, but this lets us start prototyping
		 * quickly.  By keeping all the client-callable APIs in the EntryPoints folder,
		 * hopefully it won't be too much of a goose hunt to refactor it later if we decide to do so.
		 **/
		public static Player createGame(string loadFilePath, string defaultBicPath) {
			EngineStorage.createThread();
			EngineStorage.gameDataMutex.WaitOne();

			C7SaveFormat save = SaveManager.Load(loadFilePath, defaultBicPath);
			EngineStorage.gameData = save.GameData;
			// Consider if we have any need to keep a reference to the save object handy...probably not

			// TEMPORARY: if loading a save that has no players, create dummy game data,
			// otherwise assume there is a human player in the save.
			Player humanPlayer = null;
			if (save.GameData.players.Count == 0) {
				humanPlayer = save.GameData.CreateDummyGameData();
			} else {
				humanPlayer = save.GameData.players.Find(p => p.isHuman);
			}

			EngineStorage.uiControllerID = humanPlayer.guid;
			TurnHandling.OnBeginTurn(); // Call for the first turn
			TurnHandling.AdvanceTurn();

			EngineStorage.gameDataMutex.ReleaseMutex();
			return humanPlayer;
		}
	}
}
