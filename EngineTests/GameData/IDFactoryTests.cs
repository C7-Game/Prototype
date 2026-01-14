using System.Collections.Generic;
using C7GameData;
using C7GameData.Save;
using Xunit;

namespace EngineTests.GameData;

public class IDFactoryTests {
	[Fact]
	public void CreateID_FirstID_ReturnsCorrectID() {
		var factory = new ID.Factory();

		var id = factory.CreateID("unit");

		Assert.Equal("unit-1", id.ToString());
	}

	[Fact]
	public void CreateID_MultipleIDs_IncrementCorrectly() {
		var factory = new ID.Factory();

		ID id1 = factory.CreateID("unit");
		ID id2 = factory.CreateID("unit");
		ID id3 = factory.CreateID("unit");

		Assert.Equal("unit-1", id1.ToString());
		Assert.Equal("unit-2", id2.ToString());
		Assert.Equal("unit-3", id3.ToString());
	}

	[Fact]
	public void Factory_InitializesWithSaveGame_CorrectlyCountsExistingIDs() {
		var warrior1 = new SaveUnit{id=ID.FromString("warrior-1")};
		var warrior2 = new SaveUnit{id=ID.FromString("warrior-3")};
		var worker = new SaveUnit{id=ID.FromString("worker-2")};
		var city = new SaveCity{id=ID.FromString("city-1")};

		var saveGame = new SaveGame {
			Units = new List<SaveUnit> { warrior1, warrior2, worker },
			Cities = new List<SaveCity> { city }
		};

		var factory = new ID.Factory(saveGame);

		ID newUnitID = factory.CreateID("warrior");
		ID newWorkerID = factory.CreateID("worker");
		ID newCityID = factory.CreateID("city");

		Assert.Equal("warrior-4", newUnitID.ToString());
		Assert.Equal("worker-3", newWorkerID.ToString());
		Assert.Equal("city-2", newCityID.ToString());
	}

	[Fact]
	public void Factory_HandlesEmptySaveGame_CreatesNewIDs() {
		var saveGame = new SaveGame();
		var factory = new ID.Factory(saveGame);

		ID id = factory.CreateID("unit");

		Assert.Equal("unit-1", id.ToString());
	}
}
