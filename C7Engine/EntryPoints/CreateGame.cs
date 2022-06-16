namespace C7Engine
{
	using System;
	using C7GameData;
	using C7Engine.Components;

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

			C7SaveFormat save = SaveManager.LoadSave(loadFilePath, defaultBicPath);
			EngineStorage.gameData = save.GameData;
			EngineStorage.rules = save.Rules;
			// Consider if we have any need to keep a reference to the save object handy...probably not

			var humanPlayer = save.GameData.CreateDummyGameData(EngineStorage.rules);
			EngineStorage.uiControllerID = humanPlayer.guid;
			InitializeGameComponents();
			TurnHandling.OnBeginTurn(); // Call for the first turn
			TurnHandling.AdvanceTurn();

			EngineStorage.gameDataMutex.ReleaseMutex();
			return humanPlayer;
		}

		private static void InitializeGameComponents()
		{
			ComponentManager.Instance
				.AddComponent<CalendarComponent>(new CalendarComponent(EngineStorage.gameData))
				.AddComponent<AutosaveComponent>(new AutosaveComponent(EngineStorage.gameData))
				.InitializeComponents();
		}
	}
}
