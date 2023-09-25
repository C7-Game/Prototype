using System;
using System.Linq;
using C7GameData;
using C7GameData.Save;

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

			SaveGame save = SaveManager.LoadSave(loadFilePath, defaultBicPath);
			GameData gameData = save.ToGameData();

			EngineStorage.gameData = gameData;

			// TODO: (pcen) initially, in the false branch I assigned gameData.CreateDummyGameData
			// to humanPlayer, but this is not correct since there are already players and units in
			// the .sav - instead, we should remove CreateDummyGameData and implement simple save
			// generation using the new SaveGame class. This would be difficult before due to GameData's
			// members containing numerous references to eachother, but with SaveGame, each entity is
			// only defined once in the save file, and references to it are stored as IDs making it easy
			// to generate and modify valid save files.
			Player humanPlayer = gameData.players.Any(p => p.isHuman) switch {
				true => gameData.players.Find(p => p.isHuman),
				false => throw new Exception($"{loadFilePath} does not contain a human player"),
			};

			EngineStorage.uiControllerID = humanPlayer.id;
			TurnHandling.OnBeginTurn(); // Call for the first turn
			TurnHandling.AdvanceTurn();

			EngineStorage.gameDataMutex.ReleaseMutex();
			return humanPlayer;
		}
	}
}
