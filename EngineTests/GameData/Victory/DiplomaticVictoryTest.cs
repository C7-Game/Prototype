using System.Collections.Generic;
using C7Engine;
using C7GameData;
using C7GameData.Save;
using Xunit;

namespace EngineTests.GameData.Victory;

public class DiplomaticVictoryTest {

	[Fact]
	public void HasVictory_AlwaysFalse_NotYetImplemented() {
		var victory = new DiplomaticVictory();

		Assert.False(victory.HasVictory(new VictoryStatus { OwnsUnitedNations = true }));
	}

	[Fact]
	public void Evaluate_TrueWhenPlayerOwnsAUnitedNationsBuilding() {
		Player player = new() { civilization = new Civilization("Rome") };
		City city = new City(Tile.NONE, player, "Roma", ID.None("city"));
		Building unitedNations = new Building(new SaveBuilding { name = "The United Nations" }, new C7GameData.GameData());
		city.constructed_buildings.Add(new CityBuilding { building = unitedNations });
		player.cities.Add(city);

		var victory = new DiplomaticVictory();
		VictoryStatus status = victory.Evaluate(player, new C7GameData.GameData());

		Assert.True(status.OwnsUnitedNations);
	}

	[Fact]
	public void Evaluate_FalseWhenPlayerHasNoQualifyingBuildings() {
		Player player = new() { civilization = new Civilization("Rome") };
		City city = new City(Tile.NONE, player, "Roma", ID.None("city"));
		Building temple = new Building(new SaveBuilding { name = "Temple" }, new C7GameData.GameData());
		city.constructed_buildings.Add(new CityBuilding { building = temple });
		player.cities.Add(city);

		var victory = new DiplomaticVictory();
		VictoryStatus status = victory.Evaluate(player, new C7GameData.GameData());

		Assert.False(status.OwnsUnitedNations);
	}

	[Fact]
	public void GenerateStatusRows_NoOneOwnsIt_ShowsPlaceholder() {
		var victory = new DiplomaticVictory();
		var status = new VictoryStatus { OwnsUnitedNations = false, Player = new Player { civilization = new Civilization("Rome") } };

		var rows = new List<string[]>(victory.GenerateStatusRows(status, new List<VictoryStatus>()));

		Assert.Equal("No one", rows[0][5]);
	}

	[Fact]
	public void GenerateStatusRows_RivalOwnsIt_NamesTheRival() {
		var victory = new DiplomaticVictory();
		var status = new VictoryStatus { OwnsUnitedNations = false, Player = new Player { civilization = new Civilization("Rome") } };
		var rivalOwner = new VictoryStatus {
			OwnsUnitedNations = true,
			Player = new Player { civilization = new Civilization("Greece") }
		};

		var rows = new List<string[]>(
			victory.GenerateStatusRows(status, new List<VictoryStatus> { rivalOwner }));

		Assert.Contains("Greece", rows[0][4]);
		Assert.Equal("", rows[0][5]);
	}
}
