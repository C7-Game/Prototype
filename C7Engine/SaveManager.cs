namespace C7Engine
{
	using System.IO;
	using C7GameData;

	enum SaveFileFormat {
		Sav,
		Biq,
		C7,
		Invalid,
	}

	// The engine performs all save file creating, reading, and updating
	// via the SaveManager
	public static class SaveManager
	{
		private static SaveFileFormat getFileFormat(string path)
		{
			var ext = Path.GetExtension(path);
			if (ext.Equals(".SAV", System.StringComparison.CurrentCultureIgnoreCase))
			{
				return SaveFileFormat.Sav;
			}
			else if (ext.Equals(".BIQ", System.StringComparison.CurrentCultureIgnoreCase))
			{
				return SaveFileFormat.Biq;
			}
			else if (ext.Equals(".JSON", System.StringComparison.CurrentCultureIgnoreCase)
			         || ext.Equals(".ZIP", System.StringComparison.CurrentCultureIgnoreCase))
			{
				return SaveFileFormat.C7;
			}
			return SaveFileFormat.Invalid;
		}

		// Load and initialize a save
		public static C7SaveFormat Load(string path, string bicPath)
		{
			C7SaveFormat save = null;
			switch (getFileFormat(path))
			{
			case SaveFileFormat.Sav:
				save = ImportCiv3.ImportSav(path, bicPath);
				break;
			case SaveFileFormat.Biq:
				save = ImportCiv3.ImportBiq(path, bicPath);
				break;
			case SaveFileFormat.C7:
				save = C7SaveFormat.Load(path);
				break;
			default:
				throw new FileLoadException("invalid save format");
			}
			throw new FileLoadException("could not process save file");
		}

		public static void Save(string path) {
			C7SaveFormat save = new C7SaveFormat(EngineStorage.gameData);
			C7SaveFormat.Save(save, path);
			new MsgFinishSave().send();
		}

	}
}
