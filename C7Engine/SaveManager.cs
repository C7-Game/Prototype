using System.IO;
using C7GameData;
using C7GameData.Save;

namespace C7Engine {

	enum SaveFileFormat {
		Sav,
		Biq,
		C7,
		C7Zip,
		Invalid,
	}

	// The engine performs all save file creating, reading, and updating
	// via the SaveManager
	public static class SaveManager {
		private static SaveFileFormat getFileFormat(string path) {
			return Path.GetExtension(path).ToUpper() switch {
				".SAV" => SaveFileFormat.Sav,
				".BIQ" => SaveFileFormat.Biq,
				".JSON" => SaveFileFormat.C7,
				".ZIP" => SaveFileFormat.C7Zip,
				_ => SaveFileFormat.Invalid,
			};
		}

		// Load and initialize a save
		public static SaveGame LoadSave(string path, string bicPath) {
			SaveGame save = getFileFormat(path) switch {
				SaveFileFormat.Sav => ImportCiv3.ImportSav(path, bicPath),
				SaveFileFormat.Biq => ImportCiv3.ImportBiq(path, bicPath),
				SaveFileFormat.C7 => SaveGame.Load(path, SaveCompression.None),
				SaveFileFormat.C7Zip => SaveGame.Load(path, SaveCompression.Zip),
				_ => throw new FileLoadException("invalid save format"),
			};
			return save;
		}

		public static void Save(string path, SaveCompression compression) {
			GameData gameData = EngineStorage.gameData;
			SaveGame save = SaveGame.FromGameData(gameData);
			save.Save(path, compression);
		}

	}
}
