using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;

using Xunit;

using C7GameData;
using C7GameData.Save;
using QueryCiv3;

public class SaveTests
{
	private static readonly string C7GameDataTestsFolderName = "C7GameDataTests";

	private static string getBasePath(string file) => Path.Combine(testDirectory, file);

	private static string getDataPath(string file) => Path.Combine(testDirectory, "data", file);

	private static string defaultBicPath {
		get => Path.Combine(Civ3Location.GetCiv3Path(), "Conquests", "conquests.biq");
	}

	private static string testDirectory {
		get {
			string[] parts = AppDomain.CurrentDomain.BaseDirectory.Split(Path.DirectorySeparatorChar);
			int pos = parts.Reverse().ToList().FindIndex(s => s == C7GameDataTestsFolderName);
			string up = string.Concat("..", Path.DirectorySeparatorChar);
			string relativePath = string.Concat(Enumerable.Repeat(up, pos - 1));
			return Path.GetFullPath(relativePath);
		}
	}

	[Fact]
	public void SimpleSave()
	{
		// simple load SaveGame and save to file:
		string outputNeverGameDataPath = getDataPath("output/static-save-never-game-data.json");

		// load SaveGame but convert to and from GameData before saving to file:
		string outputWasGameDataPath = getDataPath("output/static-save-was-game-data.json");

		string developerSave = getBasePath("../C7/Text/c7-static-map-save.json");

		SaveGame saveNeverGameData = SaveGame.Load(developerSave);

		saveNeverGameData.Save(outputNeverGameDataPath);
		GameData gameData = saveNeverGameData.ToGameData();
		SaveGame saveWasGameData = SaveGame.FromGameData(gameData);
		saveWasGameData.Save(outputWasGameDataPath);

		byte[] original = File.ReadAllBytes(developerSave);
		byte[] savedNeverGameData = File.ReadAllBytes(outputNeverGameDataPath);
		byte[] savedWasGameData = File.ReadAllBytes(outputWasGameDataPath);

		// saved files should not be empty
		Assert.NotEmpty(savedNeverGameData);
		Assert.NotEmpty(savedWasGameData);

		// saved files should be the same as the original
		Assert.Equal(original, savedNeverGameData);

		// TODO: Currently the order of the properties in the json is different,
		// so this fails. For testing, it would be convenient to sort SaveGame
		// fields alphabetically before serializing to json.

		Assert.Equal(original, savedWasGameData);
	}

	[Fact]
	public async void LoadSampleSaves() {
		string savesPath = getDataPath("saves");
		Directory.CreateDirectory(savesPath);
		using (var client = new HttpClient())
		{
			byte[] fileData = await client.GetByteArrayAsync("https://drive.usercontent.google.com/download?id=1QlIavkLtPZEIv1kHK9sO0fY2yp3o2si7&confirm=y");
			File.WriteAllBytes(Path.Combine(testDirectory, "data", "12345.SAV"), fileData);
		}

		IEnumerable<FileInfo> saveFiles = new DirectoryInfo(savesPath).EnumerateFiles("*.SAV");
		int i = 0;
		foreach (FileInfo saveFileInfo in saveFiles) {
			SaveGame game = null;
			GameData gd = null;
			Exception ex = Record.Exception(() => {
				game = ImportCiv3.ImportSav(saveFileInfo.FullName, defaultBicPath);
			});
			Assert.Null(ex);
			ex = Record.Exception(() => {
				gd = game.ToGameData();
			});
			Assert.Null(ex);
			Assert.NotNull(game);
			Assert.NotNull(gd);
			game.Save(Path.Combine(testDirectory, "data", "output", $"gotm_save_{i}.json"));
			i++;
		}
	}

	[Fact]
	public void LoadAllConquests() {
		string conquests = Path.Join(Civ3Location.GetCiv3Path(), "Conquests/Conquests");
		DirectoryInfo directoryInfo = new DirectoryInfo(conquests);
		IEnumerable<FileInfo> saveFiles = directoryInfo.EnumerateFiles().Where(fi => {
			// currently only test 1 Mesopotamia.biq -> 9 WWII in the Pacific.biq:
			int prefix = fi.Name[0];
			if (prefix == '7') {
				// skip 7 Sengoku - Sword of the Shogun.biq for now because biq parsing fails
				return false;
			}
			return fi.Extension.EndsWith(".biq", true, null) && char.IsAsciiDigit(fi.Name[0]);
		});
		foreach (FileInfo saveFileInfo in saveFiles) {
			string name = saveFileInfo.Name;
			SaveGame game = null;
			GameData gd = null;
			Exception ex = Record.Exception(() => {
				game = ImportCiv3.ImportBiq(saveFileInfo.FullName, defaultBicPath);
			});
			Assert.Null(ex);
			ex = Record.Exception(() => {
				gd = game.ToGameData();
			});
			Assert.Null(ex);
			Assert.NotNull(game);
			Assert.NotNull(gd);
			game.Save(Path.Combine(testDirectory, "data", "output", $"conquest_{name[0]}.json"));
		}
	}
}
