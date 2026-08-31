using System.Collections.Generic;
using C7GameData;
using Xunit;

namespace EngineTests.GameData.Victory;

public class CulturalVictoryTest {

	[Theory]
	[InlineData(100, 50, 150, 10, true)]   // total culture alone clears the bar
	[InlineData(100, 50, 50, 60, true)]    // top city culture alone clears the bar
	[InlineData(100, 50, 99, 49, false)]   // neither condition met
	[InlineData(100, 50, 100, 0, true)]    // exactly at the total-culture limit
	[InlineData(100, 50, 0, 50, true)]     // exactly at the city-culture limit
	public void HasVictory_TrueWhenEitherTotalOrTopCityCultureMeetsLimit(
		int totalLimit, int cityLimit, int totalCulture, int topCityCulture, bool expected) {

		var victory = new CulturalVictory(totalLimit, cityLimit);
		var status = new VictoryStatus { TotalCulture = totalCulture, TopCityCulture = topCityCulture };

		Assert.Equal(expected, victory.HasVictory(status));
	}

	[Fact]
	public void Evaluate_UsesHistoryForTotalCultureAndBestCityForTopCityCulture() {
		Player player = new() { civilization = new Civilization("Rome"), id = ID.FromString("player-1") };

		City weakerCity = new City(Tile.NONE, player, "Ravenna", ID.None("city"));
		weakerCity.perPlayerCulture[player] = 20;
		City strongerCity = new City(Tile.NONE, player, "Roma", ID.None("city"));
		strongerCity.perPlayerCulture[player] = 75;
		player.cities.Add(weakerCity);
		player.cities.Add(strongerCity);

		C7GameData.GameData gameData = new() {
			history = new Dictionary<string, List<HistTurnRecord>> {
				[player.id.ToString()] = new() { new HistTurnRecord { Culture = 250 } }
			}
		};

		var victory = new CulturalVictory(totalCultureLimit: 300, cityCultureLimit: 100);
		VictoryStatus status = victory.Evaluate(player, gameData);

		Assert.Equal(250, status.TotalCulture);
		Assert.Equal(75, status.TopCityCulture);
		Assert.Equal("Roma", status.TopCityName);
		Assert.False(victory.HasVictory(status));
	}

	[Fact]
	public void Evaluate_NoCities_ReportsNoneAndZeroCulture() {
		Player player = new() { civilization = new Civilization("Rome"), id = ID.FromString("player-1") };

		C7GameData.GameData gameData = new() {
			history = new Dictionary<string, List<HistTurnRecord>> {
				[player.id.ToString()] = new()
			}
		};

		var victory = new CulturalVictory(totalCultureLimit: 300, cityCultureLimit: 100);
		VictoryStatus status = victory.Evaluate(player, gameData);

		Assert.Equal(0, status.TotalCulture);
		Assert.Equal(0, status.TopCityCulture);
		Assert.Equal("None", status.TopCityName);
	}

	[Fact]
	public void GenerateStatusRows_OmitsRivalCityValueWhenRivalHasNoCulture() {
		var victory = new CulturalVictory(totalCultureLimit: 300, cityCultureLimit: 100);
		var status = new VictoryStatus { TopCityName = "Roma", TopCityCulture = 40, TotalCulture = 40 };
		var rivalWithNoCulture = new VictoryStatus {
			Player = new Player { civilization = new Civilization("Greece") },
			TopCityName = "Athens",
			TopCityCulture = 0,
			TotalCulture = 0
		};

		var rows = new List<string[]>(
			victory.GenerateStatusRows(status, new List<VictoryStatus> { rivalWithNoCulture }));

		// Top-city row: rival culture of 0 should render as blank, not "0".
		Assert.Equal("", rows[0][5]);
		// Total-culture row: same rule.
		Assert.Equal("", rows[1][5]);
	}
}
