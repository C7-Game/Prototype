using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

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
			string up = String.Concat("..", Path.DirectorySeparatorChar);
			string relativePath = String.Concat(Enumerable.Repeat(up, pos - 1));
			return Path.GetFullPath(relativePath);
		}
	}

	[Fact]
	public void SimpleSave()
	{
		C7SaveFormat sav = C7SaveFormat.Load(getBasePath("../C7/Text/c7-static-map-save.json"));
		sav.GameData.map.computeNeighbors();
		SaveGame save = SaveGame.FromGameData(sav.GameData);
		save.Save(getBasePath("./save.json"));
	}

	[Fact]
	public void LoadGOTMWinners() {
		string path = getDataPath("gotm");
		DirectoryInfo directoryInfo = new DirectoryInfo(path);
		IEnumerable<FileInfo> saveFiles = directoryInfo.EnumerateFiles();
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
			game.Save(Path.Combine(testDirectory, "data", "output", $"save_{i}.json"));
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
			return fi.Extension.EndsWith(".biq", true, null) && Char.IsAsciiDigit(fi.Name[0]);
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
