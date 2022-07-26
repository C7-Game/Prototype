using C7GameData;
using Xunit;

public class CityTest
{
	[Fact]
	public void CityWith2ProductionPerTurn_ShouldReturn1TurnIf9_of_10ProductionDone()
	{
		UnitPrototype warrior = new UnitPrototype();
		warrior.shieldCost = 10;

		City city = new City(Tile.NONE, null, "Fighter Town, USA");
		city.itemBeingProduced = warrior;
		city.shieldsStored = 9;

		TerrainType oneShield = new TerrainType();
		oneShield.baseShieldProduction = 1;

		Tile workedTile = new Tile();
		workedTile.overlayTerrainType = oneShield;

		CityResident maverick = new CityResident();
		maverick.tileWorked = workedTile;
		city.residents.Add(maverick);

		int turnsUntilFinished = city.TurnsUntilProductionFinished();
		Assert.Equal(1, turnsUntilFinished);
	}

	[Fact]
	public void CityWith2ProductionPerTurn_ShouldReturn1TurnIf19_of_20FoodDone() {
		City city = new City(Tile.NONE, null, "Gotham");
		city.foodStored = 19;
		city.size = 1;

		TerrainType grassland = new TerrainType();
		grassland.baseFoodProduction = 2;

		Tile workedTile = new Tile();
		workedTile.overlayTerrainType = grassland;

		CityResident robin = new CityResident();
		robin.tileWorked = workedTile;
		city.residents.Add(robin);

		int turnsUntilGrowth = city.TurnsUntilGrowth();
		Assert.Equal(1, turnsUntilGrowth);
	}
}
