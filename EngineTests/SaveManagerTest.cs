using System;
using C7Engine;
using C7GameData;
using Xunit;

namespace EngineTests {

	public class SaveManagerTest {

		private readonly string jsonSavePath = Utils.DataPath + "save.json";
		private readonly string zipSavePath = Utils.DataPath + "save.zip";
		// private readonly string savSavePath = Utils.DataPath + "save.sav";

		[Fact]
		public void SaveManager_LoadsJsonSaveSuccessfully() {
			C7SaveFormat save = null;
			Exception ex = Record.Exception(() => save = SaveManager.LoadSave(jsonSavePath));

			Assert.Null(ex);
			Assert.NotNull(save);
			Assert.Equal("test-version-string", save.Version);
			Assert.Null(save.Rules);
			Assert.Equal(80, save.GameData.map.numTilesTall);
			Assert.Equal(3200, save.GameData.map.tiles.Count);
			Assert.Equal(14, save.GameData.terrainTypes.Count);
		}

		[Fact]
		public void SaveManager_LoadsZipSaveSuccessfully() {
			C7SaveFormat save = null;
			Exception ex = Record.Exception(() => save = SaveManager.LoadSave(zipSavePath));

			Assert.Null(ex);
			Assert.NotNull(save);
			Assert.Equal("test-version-string", save.Version);
			Assert.Null(save.Rules);
			Assert.Equal(80, save.GameData.map.numTilesTall);
			Assert.Equal(3200, save.GameData.map.tiles.Count);
			Assert.Equal(14, save.GameData.terrainTypes.Count);
		}
	}
}
