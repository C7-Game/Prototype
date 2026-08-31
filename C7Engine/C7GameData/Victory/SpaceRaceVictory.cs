using System.Collections.Generic;
using C7GameData;

namespace C7Engine;

public class SpaceRaceVictory : IVictory {
	private readonly int _partsToBuild;

	public SpaceRaceVictory(int partsToBuild) {
		_partsToBuild = partsToBuild;
	}

	public string Header() => "Space Race";

	public VictoryStatus Evaluate(Player player, GameData gameData) {
		// var spaceShipParts = player.cities.Count(c => c.GetBuildings().Any(b => b.isSpaceShipPart));

		return new VictoryStatus {
			Player = player
		};
	}

	public bool HasVictory(VictoryStatus status) {
		return false; // TODO: Space Race Victory
	}

	public IEnumerable<string[]> GenerateStatusRows(VictoryStatus status, List<VictoryStatus> rivalStatuses) {
		yield return SpaceRacePrint(status, rivalStatuses);
	}

	private string[] SpaceRacePrint(VictoryStatus status, List<VictoryStatus> rivalStatuses) {

		return [
			"Parts to build",
			$"{_partsToBuild}",
			"Parts built:",
			"??",
			"",
			""
		];
	}
}
