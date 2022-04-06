using System;
using C7Engine;
using C7GameData;
using Xunit;

namespace EngineTests {

	public class SaveManagerTest {

		private readonly string jsonSavePath = Utils.DataPath + "save.json";
		private readonly string zipSavePath = Utils.DataPath + "save.zip";
		private readonly string invalidTypeSavePath = Utils.DataPath + "save.foo";
		private readonly string nonexistentSavePath = Utils.DataPath + "save.nonexistent";
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

		[Fact]
		public void SaveManager_ExceptionOnInvalidFileType() {
			C7SaveFormat save1 = null;
			Exception ex1 = Record.Exception(() => save1 = SaveManager.LoadSave(invalidTypeSavePath));

			Assert.NotNull(ex1);
			Assert.Null(save1);

			C7SaveFormat save2 = null;
			Exception ex2 = Record.Exception(() => save2 = SaveManager.LoadSave(Utils.DataPath));

			Assert.NotNull(ex2);
			Assert.Null(save2);
		}

		[Fact]
		public void SaveManager_ExceptionOnNonexistentFile() {
			C7SaveFormat save = null;
			Exception ex = Record.Exception(() => save = SaveManager.LoadSave(nonexistentSavePath));

			Assert.Null(save);
			Assert.NotNull(ex);
		}

	}
}
