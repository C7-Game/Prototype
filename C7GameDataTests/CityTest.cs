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
}
