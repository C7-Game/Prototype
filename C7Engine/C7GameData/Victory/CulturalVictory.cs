using System.Collections.Generic;
using System.Linq;

namespace C7GameData;

public class CulturalVictory : IVictory {
	private readonly float _totalCultureLimit;
	private readonly float _cityCultureLimit;

	public CulturalVictory(int totalCultureLimit, int cityCultureLimit) {
		_totalCultureLimit = totalCultureLimit;
		_cityCultureLimit = cityCultureLimit;
	}

	public string Header() => "Cultural";

	public VictoryStatus Evaluate(Player player, GameData gameData) {
		var citiesByCulture = player.cities
			.Select(c => new { Name = c.name, Culture = c.GetCulture()})
			.OrderByDescending(c => c.Culture)
			.ToList();

		var topCityCulture = citiesByCulture.FirstOrDefault();

		// Not the same as total civ culture: ignores culture gained from lost cities
		// var totalCityCulture = citiesByCulture.Sum(c => c.Culture);

		var history = gameData.history[player.id.ToString()];
		var lastTurn = history.LastOrDefault();
		var totalAccumulatedCulture = lastTurn?.Culture ?? 0;

		return new VictoryStatus {
			Player = player,
			TotalCulture = totalAccumulatedCulture,
			TopCityCulture = topCityCulture?.Culture ?? 0,
			TopCityName = topCityCulture?.Name ?? "None"
		};
	}

	public bool HasVictory(VictoryStatus status) {
		return status.TotalCulture >= _totalCultureLimit
			   || status.TopCityCulture >= _cityCultureLimit;
	}

	public IEnumerable<string[]> GenerateStatusRows(VictoryStatus status, List<VictoryStatus> rivalStatuses) {
		yield return TopCityCulturePrint(status, rivalStatuses);
		yield return TotalCulturePrint(status, rivalStatuses);
	}

	private string[] TopCityCulturePrint(VictoryStatus status, List<VictoryStatus> rivalStatuses) {

		VictoryStatus topRivalByCultureOneCity = rivalStatuses
			.OrderByDescending(r => r.TopCityCulture).FirstOrDefault();

		string topCultureRival = topRivalByCultureOneCity?.Player?.civilization?.name ?? "";
		string topCultureRivalCity = topRivalByCultureOneCity?.TopCityName ?? "";
		string topCultureRivalCityString = topRivalByCultureOneCity?.TopCityName == null
			? "" : $"{topCultureRivalCity} ({topCultureRival})";

		int topCultureRivalCityValue = topRivalByCultureOneCity?.TopCityCulture ?? 0;
		string topCultureRivalCityValueString = topCultureRivalCityValue > 0 ? $"{topCultureRivalCityValue}" : "";

		return [
			"One city",
			$"{_cityCultureLimit}",
			status.TopCityName,
			$"{status.TopCityCulture}",
			topCultureRivalCityString,
			topCultureRivalCityValueString
			];
	}

	private string[] TotalCulturePrint(VictoryStatus status, List<VictoryStatus> rivalStatuses) {
		VictoryStatus topRivalByTotalCulture = rivalStatuses
			.OrderByDescending(r => r.TotalCulture).FirstOrDefault();

		string topCultureRival = topRivalByTotalCulture?.Player?.civilization?.name ?? "";
		int topCultureRivalValue = topRivalByTotalCulture?.TotalCulture ?? 0;
		string topCultureRivalValueString =  topCultureRivalValue > 0 ? $"{topCultureRivalValue}" : "";

		return [
			"Entire civilization",
			$"{_totalCultureLimit}",
			"Entire civilization",
			$"{status.TotalCulture}",
			topCultureRival,
			topCultureRivalValueString
		];
	}
}
