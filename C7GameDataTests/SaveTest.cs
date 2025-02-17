using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;

using Xunit;

using C7GameData;
using C7GameData.Save;
using QueryCiv3;
using System.Runtime.InteropServices;

public class SaveTests {

	private static readonly string C7GameDataTestsFolderName = "C7GameDataTests";

	private static string getBasePath(string file) => Path.Combine(testDirectory, file);

	private static string getDataPath(string file) => Path.Combine(testDirectory, "data", file);

	private static string defaultBicPath {
		get => Path.Combine(Civ3Location.GetCiv3Path(), "Conquests", "conquests.biq");
	}

	private static string defaultPediaIconsPath {
		get => Path.Combine(Civ3Location.GetCiv3Path(), "Conquests", "Text", "PediaIcons.txt");
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

	private static string GetMd5FileHash(string path) {
		if (!File.Exists(path)) {
			return "";
		}
		using MD5 md5 = MD5.Create();
		using FileStream fileStream = File.OpenRead(path);
		byte[] hashBytes = md5.ComputeHash(fileStream);
		return BitConverter.ToString(hashBytes).Replace("-", "").ToLower();
	}

	[Fact]
	public void SimpleSave() {
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

		string[] original = File.ReadAllLines(developerSave);
		string[] savedNeverGameData = File.ReadAllLines(outputNeverGameDataPath);
		string[] savedWasGameData = File.ReadAllLines(outputWasGameDataPath);

		// saved files should not be empty
		Assert.NotEmpty(savedNeverGameData);
		Assert.NotEmpty(savedWasGameData);

		// saved files should be the same as the original
		Assert.Equal(original, savedNeverGameData);
		Assert.Equal(original, savedWasGameData);
	}

	[Fact]
	public async void LoadSampleSaves() {
		// When running the tests via github actions, civ3 isn't installed so we
		// can't load the default bic.
		//
		// See https://docs.github.com/en/actions/writing-workflows/choosing-what-your-workflow-does/store-information-in-variables#default-environment-variables
		// for a full list of env vars.
		string is_on_github = System.Environment.GetEnvironmentVariable("CI");
		if (is_on_github != null) { return; }

		string savesPath = getDataPath("saves");
		Directory.CreateDirectory(savesPath);

		string sampleSavPath = Path.Combine(savesPath, "12345.SAV");
		if (GetMd5FileHash(sampleSavPath) != "d34dd19a76eaebe26d29d73132c2fa60") {
			using HttpClient client = new();
			byte[] fileData = await client.GetByteArrayAsync("https://drive.usercontent.google.com/download?id=1QlIavkLtPZEIv1kHK9sO0fY2yp3o2si7&confirm=y");
			File.WriteAllBytes(sampleSavPath, fileData);
		}

		IEnumerable<FileInfo> saveFiles = new DirectoryInfo(savesPath).EnumerateFiles("*.SAV");
		int i = 0;
		foreach (FileInfo saveFileInfo in saveFiles) {
			SaveGame game = null;
			GameData gd = null;
			Exception ex = Record.Exception(() => {
				game = ImportCiv3.ImportSav(saveFileInfo.FullName, defaultBicPath, (relativeModePath) => {
					return defaultPediaIconsPath;
				});
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
		Assert.True(i > 0);
	}

	[Fact]
	public void LoadAllConquestScenarios() {
		// When running the tests via github actions, civ3 isn't installed so we can't
		// check the conquests directories.
		//
		// See https://docs.github.com/en/actions/writing-workflows/choosing-what-your-workflow-does/store-information-in-variables#default-environment-variables
		// for a full list of env vars.
		string is_on_github = System.Environment.GetEnvironmentVariable("CI");
		if (is_on_github != null) { return; }

		CheckScenariosInCiv3Subfolder("Conquests/Conquests");
		CheckScenariosInCiv3Subfolder("Conquests/Scenarios");
	}

	public void CheckScenariosInCiv3Subfolder(string subfolder) {
		string conquests = Path.Join(Civ3Location.GetCiv3Path(), subfolder);
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
				game = ImportCiv3.ImportBiq(saveFileInfo.FullName, defaultBicPath, (relativeModPath) => {
					if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
						relativeModPath = relativeModPath.Replace("\\conquests\\", "/Conquests/");
					}

					return Path.GetFullPath(Path.Combine(Civ3Location.GetCiv3Path(), subfolder, relativeModPath, "Text", "PediaIcons.txt"));
				});
			});
			Assert.True(ex == null, name + ": " + ex?.ToString());
			ex = Record.Exception(() => {
				gd = game.ToGameData();
			});
			Assert.True(ex == null, name + ":" + ex?.ToString());
			Assert.NotNull(game);
			Assert.NotNull(gd);

			// Check that the human player has at least one settler or city in
			// each scenario, when looking at the SaveGame.
			foreach (SavePlayer player in game.Players) {
				int settlerCount = 0;
				int totalUnitCount = 0;
				foreach (SaveUnit su in game.Units) {
					if (su.owner == player.id) {
						++totalUnitCount;
						if (su.prototype == "Settler") {
							++settlerCount;
						}
					}
				}

				int cityCount = 0;
				foreach (SaveCity sc in game.Cities) {
					if (sc.owner == player.id) {
						++cityCount;
					}
				}

				// This debugging output is visible when run via
				// `dotnet test -v n`
				Console.WriteLine(name + " : " + player.civilization + " has " +
									settlerCount + " settlers, " +
									cityCount + " cities, " +
									totalUnitCount + " units, and is " +
									(player.human ? "" : "not ") +
									"the human player");

				// The human player should always have either a city or a settler.
				if (player.human) {
					Assert.True(cityCount + settlerCount > 0,
								name + " : " + player.civilization);
				}
			}

			// And check again, looking at the GameData.
			foreach (Player player in gd.players) {
				int settlerCount = 0;
				int totalUnitCount = 0;
				foreach (MapUnit mu in player.units) {
					++totalUnitCount;
					if (mu.unitType.name == "Settler") {
						++settlerCount;
					}
				}

				int cityCount = player.cities.Count;

				Console.WriteLine(name + " : " + player.civilization.name + " has " +
									settlerCount + " settlers, " +
									cityCount + " cities, " +
									totalUnitCount + " units, and is " +
									(player.isHuman ? "" : "not ") +
									"the human player");

				// The human player should always have either a city or a settler.
				if (player.isHuman) {
					Assert.True(cityCount + settlerCount > 0,
								name + " : " + player.civilization.name);
				}
			}

			game.Save(Path.Combine(testDirectory, "data", "output", $"conquest_{name[0]}.json"));
		}
	}
}
