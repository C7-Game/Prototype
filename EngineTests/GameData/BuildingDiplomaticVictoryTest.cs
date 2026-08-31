using C7GameData;
using C7GameData.Save;
using Xunit;

namespace EngineTests.GameData;

public class BuildingDiplomaticVictoryTest {

	private static Building BuildingNamed(string name) {
		return new Building(new SaveBuilding { name = name }, new C7GameData.GameData());
	}

	[Fact]
	public void CanTriggerDiplomaticVictoryVote_TrueOnlyForTheUnitedNations() {
		Building unitedNations = BuildingNamed("The United Nations");
		Building temple = BuildingNamed("Temple");

		Assert.True(unitedNations.CanTriggerDiplomaticVictoryVote);
		Assert.False(temple.CanTriggerDiplomaticVictoryVote);
	}
}
