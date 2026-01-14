using C7GameData;
using Xunit;

namespace EngineTests.GameData;

public class CityTest {
	[Fact]
	public void CityWith2ProductionPerTurn_ShouldReturn1TurnIf9_of_10ProductionDone() {
		C7Engine.EngineStorage.InitializeGameDataForTests(new C7GameData.GameData() {
			gameDifficulty = new Difficulty(),
		});
		Player player = new() {
			isHuman = true,
		};
		player.civilization = new Civilization();
		player.government = new Government();
		UnitPrototype warrior = new UnitPrototype();
		warrior.shieldCost = 10;

		City city = new City(Tile.NONE, player, "Fighter Town, USA", ID.None("city"));
		city.itemBeingProduced = warrior;
		city.shieldsStored = 9;

		TerrainType oneShield = new TerrainType();
		oneShield.baseShieldProduction = 1;

		Tile workedTile = new Tile(ID.None("tile"));
		workedTile.overlayTerrainType = oneShield;

		CityResident maverick = new CityResident();
		maverick.tileWorked = workedTile;
		city.residents.Add(maverick);

		int turnsUntilFinished = city.TurnsUntilProductionFinished();
		Assert.Equal(1, turnsUntilFinished);
	}

	[Fact]
	public void CityWith2ProductionPerTurn_ShouldReturn1TurnIf19_of_20FoodDone() {
		Player player = new();
		player.government = new Government();
		player.rules = new() { MaximumLevel1CitySize = 6 };
		TerrainType oneShield = new TerrainType();
		oneShield.baseShieldProduction = 1;
		Tile tile = new Tile(ID.None("tile"));

		City city = new City(tile, player, "Gotham", ID.None("city"));
		city.foodStored = 19;
		tile.cityAtTile = city;

		TerrainType grassland = new TerrainType();
		grassland.baseFoodProduction = 2;

		Tile workedTile = new Tile(ID.None("test-tile"));
		workedTile.overlayTerrainType = grassland;

		CityResident robin = new CityResident();
		robin.tileWorked = workedTile;
		city.residents.Add(robin);

		int turnsUntilGrowth = city.TurnsUntilGrowth();
		Assert.Equal(1, turnsUntilGrowth);
	}

	[Fact]
	public void CityShouldShrinkWhenItRunsOutOfFood() {
		C7GameData.GameData gameData = new();
		Player player = new();
		player.government = new Government();
		player.rules = new() { MaximumLevel1CitySize = 6 };
		TerrainType oneShield = new TerrainType();
		oneShield.baseShieldProduction = 1;
		Tile tile = new Tile(ID.None("tile"));

		City city = new City(tile, player, "Gotham", ID.None("city"));
		city.foodStored = 0;
		tile.cityAtTile = city;

		CityResident resident1 = new();
		city.residents.Add(resident1);

		CityResident resident2 = new();
		city.residents.Add(resident2);

		// Confirm we lose population without enough food.
		Assert.Equal(2, city.residents.Count);
		city.HandleCityGrowth(gameData);
		Assert.Equal(1, city.residents.Count);
	}
}
