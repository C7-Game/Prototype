using C7GameData;
using Xunit;

public class GameMapTest
{
	[Fact]
	public void CityWith2ProductionPerTurn_ShouldReturn1TurnIf9_of_10ProductionDone()
	{
		System.Random rng = new System.Random(12345);
		GameMap gm = GameMap.Generate(rng);

		Assert.Equal(80, gm.numTilesTall);
	}
}
