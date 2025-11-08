using System;
using System.Linq;
using System.Threading.Tasks;
using C7GameData;
using C7GameData.Save;

namespace C7Engine;

public class CreateGameParams {
	public string LuaRulesDir;
	public string DefaultBicPath;

	public Func<string, string> GetPediaIconsPath = s => s;

	public CreateGameParams(string LuaRulesDir, string DefaultBicPath) {
		this.LuaRulesDir = LuaRulesDir;
		this.DefaultBicPath = DefaultBicPath;
	}
}

public class CreateGame {
	/**
		* For now, I'm making the methods that the C7 client can call be static.
		* We may want a different solution in the end, but this lets us start prototyping
		* quickly.  By keeping all the client-callable APIs in the EntryPoints folder,
		* hopefully it won't be too much of a goose hunt to refactor it later if we decide to do so.
		**/
	public static async Task<Player> createGame(string loadFilePath, CreateGameParams options) {
		SaveGame save = SaveManager.LoadSave(loadFilePath, options.DefaultBicPath, options.GetPediaIconsPath);
		return await createGame(save, options);
	}

	public static async Task<Player> createGame(SaveGame save, CreateGameParams options) {
		GameData gameData = save.ToGameData(options.LuaRulesDir);

		EngineStorage.gameData = gameData;
		EngineStorage.gameData.onGameCreation();

		// TODO: (pcen) initially, in the false branch I assigned gameData.CreateDummyGameData
		// to humanPlayer, but this is not correct since there are already players and units in
		// the .sav - instead, we should remove CreateDummyGameData and implement simple save
		// generation using the new SaveGame class. This would be difficult before due to GameData's
		// members containing numerous references to eachother, but with SaveGame, each entity is
		// only defined once in the save file, and references to it are stored as IDs making it easy
		// to generate and modify valid save files.
		Player humanPlayer = gameData.players.Any(p => p.isHuman) switch {
			true => gameData.players.Find(p => p.isHuman),
			false => throw new Exception($"The provided save does not contain a human player"),
		};

		EngineStorage.uiControllerID = humanPlayer.id;
		TurnHandling.OnBeginTurn(); // Call for the first turn
		await TurnHandling.AdvanceTurn();

		return humanPlayer;
	}
}
