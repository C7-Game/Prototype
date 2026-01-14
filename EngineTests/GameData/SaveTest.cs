using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using C7Engine;
using C7GameData;
using C7GameData.Save;
using Newtonsoft.Json.Linq;
using QueryCiv3;
using Xunit;

namespace EngineTests.GameData;

public class SaveTests {

	private static readonly string C7GameDataTestsFolderName = "EngineTests";

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

	private static string luaRulesDir => getBasePath("../C7/Lua/rules");

	private static string GetMd5FileHash(string path) {
		if (!File.Exists(path)) {
			return "";
		}
		using MD5 md5 = MD5.Create();
		using FileStream fileStream = File.OpenRead(path);
		byte[] hashBytes = md5.ComputeHash(fileStream);
		return BitConverter.ToString(hashBytes).Replace("-", "").ToLower();
	}

	[Theory]
	[InlineData("../C7/Text/c7-static-map-save.json", "basic")]
	[InlineData("../C7/Text/c7-static-map-save-standalone.json", "standalone")]
	public void SimpleSave(string developerSavePath, string outputFilePostfix) {
		// simple load SaveGame and save to file:
		string outputNeverGameDataPath = getDataPath($"output/static-save-never-game-data-{outputFilePostfix}.json");

		// load SaveGame but convert to and from GameData before saving to file:
		string outputWasGameDataPath = getDataPath($"output/static-save-was-game-data-{outputFilePostfix}.json");

		string developerSave = getBasePath(developerSavePath);

		SaveGame saveNeverGameData = SaveGame.Load(developerSave, (string unused) => { return unused; });

		saveNeverGameData.Save(outputNeverGameDataPath);
		C7GameData.GameData gameData = ToGameData(saveNeverGameData);
		SaveGame saveWasGameData = SaveGame.FromGameData(gameData);
		saveWasGameData.Save(outputWasGameDataPath);

		string[] original = File.ReadAllLines(developerSave);
		string[] savedNeverGameData = File.ReadAllLines(outputNeverGameDataPath);
		string[] savedWasGameData = File.ReadAllLines(outputWasGameDataPath);

		// saved files should not be empty
		Assert.NotEmpty(savedNeverGameData);
		Assert.NotEmpty(savedWasGameData);

		string originalText = File.ReadAllText(developerSave);
		string neverGameDataText = File.ReadAllText(outputNeverGameDataPath);
		string wasGameDataText = File.ReadAllText(outputWasGameDataPath);

		JObject originalJson = JObject.Parse(originalText);
		JObject neverGameDataJson = JObject.Parse(neverGameDataText);
		JObject wasGameDataJson = JObject.Parse(wasGameDataText);

		// saved files should be the same as the original
		Assert.True(JToken.DeepEquals(originalJson, neverGameDataJson));
		Assert.True(JToken.DeepEquals(originalJson, wasGameDataJson));
	}

	private void WaitForStartTurnMessage() {
		while (true) {
			EngineStorage.ProcessNextMessageToEngine();

			while (EngineStorage.TryDequeueNextMessageToUI(out MessageToUI msg)) {
				switch (msg) {
					case MsgStartTurn mST:
						return;
					default:
						continue;
				}
			}
		}
	}

	private Player CreateHeadlessGame(string path, string biqPath = "", Func<string, string> getPediaIconsPath = null) {
		CreateGameParams options = new(luaRulesDir, biqPath);
		if (getPediaIconsPath != null) {
			options.GetPediaIconsPath = getPediaIconsPath;
		}

		return CreateGame.createGame(path, options).Result;
	}

	private C7GameData.GameData ToGameData(SaveGame game) {
		return game.ToGameData(luaRulesDir);
	}

	private void CheckAiInvariants() {
		EngineStorage.ReadGameData((C7GameData.GameData gameData) => {
			C7GameData.GameData game = gameData;

			foreach (Player p in game.players) {
				foreach (MapUnit u in p.units) {
					if (u.unitType.name != "Settler") {
						continue;
					}

					// We don't require an escort if the settler is in a city.
					if (u.location.cityAtTile != null) {
						continue;
					}

					// This is a settler not in a city - make sure it has an escort.
					if (u.currentAI is SettlerAI settlerAi) {
						if (settlerAi.data.escort != null) {
							Assert.Equal(settlerAi.data.escort.location, u.location);
						} else {
							// This assertion is tempting, but will sometimes fire
							// if the escort is disbanded due to unit support costs.
							//
							// Assert.True(u.location.unitsOnTile.Count > 1, $"{u} {u.location}");
						}
					}
				}
			}
		});
	}

	[Theory]
	[InlineData("../C7/Text/c7-static-map-save.json", "basic")]
	[InlineData("../C7/Text/c7-static-map-save-standalone.json", "standalone")]
	public void SimpleGame(string developerSavePath, string outputFilePostfix) {
		string developerSave = getBasePath(developerSavePath);

		new MsgSetAnimationsEnabled(false).send();

		Player human = CreateHeadlessGame(developerSave);

		// Make all the players AI players while we run the game in headless mode.
		human.isHuman = false;

		// Play out 50 turns.
		for (int i = 0; i < 50; ++i) {
			WaitForStartTurnMessage();
			CheckAiInvariants();
			new MsgEndTurn().send();
		}
		WaitForStartTurnMessage();

		// Make the player a human again so we can save and load the game.
		human.isHuman = true;
		C7GameData.GameData game = null;
		EngineStorage.ReadGameData((C7GameData.GameData gameData) => {
			game = gameData;
		});
		Assert.Equal(50, game.turn);

		// Save the game.
		string outputDirectSavePath = getDataPath($"output/headless-game-direct-save-{outputFilePostfix}.json");
		SaveGame.FromGameData(game).Save(outputDirectSavePath);

		// Load the saved game and save it again.
		string roundTrippedSavePath = getDataPath($"output/headless-game-round-tripped-save-{outputFilePostfix}.json");
		C7GameData.GameData roundTrippedGameData = ToGameData(SaveGame.Load(outputDirectSavePath, (string unused) => { return unused; }));
		SaveGame.FromGameData(roundTrippedGameData).Save(roundTrippedSavePath);

		string[] directSaveLines = File.ReadAllLines(outputDirectSavePath);
		string[] roundTrippedSaveLines = File.ReadAllLines(roundTrippedSavePath);

		// The saved files should not be empty
		Assert.NotEmpty(directSaveLines);
		Assert.NotEmpty(roundTrippedSaveLines);

		// And they should be the same, despite round-tripping through the GameData format.
		Assert.Equal(directSaveLines, roundTrippedSaveLines);
	}

	[Fact]
	public void WorldWrapDetails() {
		string developerSave = getBasePath("../C7/Text/c7-static-map-save.json");
		Player human = CreateHeadlessGame(developerSave);
		WaitForStartTurnMessage();

		C7GameData.GameData gd = null;
		EngineStorage.ReadGameData((C7GameData.GameData gameData) => { gd = gameData; });

		Tile t0 = gd.map.tileAt(97, 33);
		Tile t1 = gd.map.tileAt(99, 33);
		Tile t2 = gd.map.tileAt(1, 33);

		Assert.Equal(t0.distanceTo(t1), 1);
		Assert.Equal(t1.distanceTo(t0), 1);

		Assert.Equal(t1.distanceTo(t2), 1);
		Assert.Equal(t2.distanceTo(t1), 1);

		Assert.Equal(t0.distanceTo(t2), 2);
		Assert.Equal(t2.distanceTo(t0), 2);
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
			C7GameData.GameData gd = null;
			Console.WriteLine(saveFileInfo.FullName);
			Exception ex = Record.Exception(() => {
				game = ImportCiv3.ImportSav(saveFileInfo.FullName, defaultBicPath, (relativeModePath) => {
					return defaultPediaIconsPath;
				});
			});
			Assert.Null(ex);
			ex = Record.Exception(() => {
				gd = ToGameData(game);
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

		string[] singleplayerScenarios = {
			"1 Mesopotamia.biq",
			"2 Rise of Rome.biq",
			"3 Fall of Rome.biq",
			"4 Middle Ages.biq",
			"5 Mesoamerica.biq",
			"6 Age of Discovery.biq",
			// skip for now because BIQ parsing fails
			// "7 Sengoku - Sword of the Shogun.biq",
			"8 Napoleonic Europe.biq",
			"9 WWII in the Pacific.biq",
		};
		string[] multiplayerScenarios = {
			"1 MP Mesopotamia.biq",
			"2 MP Rise of Rome.biq",
			"3 MP Fall of Rome.biq",
			"4 MP Middle Ages.biq",
			"5 MP Mesoamerica.biq",
			"6 MP Age of Discovery.biq",
			// skip for now because BIQ parsing fails
			// "7 MP Sengoku - Sword of the Shogun.biq",
			"8 MP Napoleonic Europe.biq",
			"9 MP WWII in the Pacific.biq",
		};
		// Only bother running one turn of the newer scenarios, just to keep the
		// tests faster.
		CheckScenariosInCiv3Subfolder("Conquests/Conquests", singleplayerScenarios, runOneTurn: true, "singleplayer");
		CheckScenariosInCiv3Subfolder("Conquests/Scenarios", multiplayerScenarios, runOneTurn: false, "multiplayer");
	}

	private void CheckScenariosInCiv3Subfolder(string subfolder, string[] scenarioNamesToTest, bool runOneTurn, string basename) {
		string conquests = Path.Join(Civ3Location.GetCiv3Path(), subfolder);
		DirectoryInfo directoryInfo = new DirectoryInfo(conquests);
		IEnumerable<FileInfo> saveFiles = directoryInfo.EnumerateFiles().Where(fi => scenarioNamesToTest.Contains(fi.Name));
		Assert.True(
			scenarioNamesToTest.Count() == saveFiles.Count(),
			$"Expected {scenarioNamesToTest.Count()} files but got {saveFiles.Count()}"
		);
		foreach (FileInfo saveFileInfo in saveFiles) {
			string name = saveFileInfo.Name;
			SaveGame game = null;
			C7GameData.GameData gd = null;

			Func<string, string> getPediaIconsPath = (relativeModPath) => {
				if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
					relativeModPath = relativeModPath.Replace("\\conquests\\", "/Conquests/");
				}

				return Path.GetFullPath(Path.Combine(Civ3Location.GetCiv3Path(), subfolder, relativeModPath, "Text", "PediaIcons.txt"));
			};

			Exception ex = Record.Exception(() => {
				game = ImportCiv3.ImportBiq(saveFileInfo.FullName, defaultBicPath, getPediaIconsPath);
			});
			Assert.True(ex == null, name + ": " + ex?.ToString());
			ex = Record.Exception(() => {
				gd = ToGameData(game);
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

				// The human player should always have either a city or a settler.
				if (player.isHuman) {
					Assert.True(cityCount + settlerCount > 0,
						name + " : " + player.civilization.name);
				}
			}

			game.Save(Path.Combine(testDirectory, "data", "output", $"{basename}_{name[0]}.json"));

			// Finally, ensure we can run the first turn of the scenario.
			if (runOneTurn) {
				Player human = CreateHeadlessGame(saveFileInfo.FullName, defaultBicPath, getPediaIconsPath);

				// Make all the players AI players while we run the game in headless mode.
				human.isHuman = false;

				WaitForStartTurnMessage();
				new MsgEndTurn().send();
				WaitForStartTurnMessage();
			}
		}
	}
}
