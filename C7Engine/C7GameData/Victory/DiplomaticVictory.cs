using System.Collections.Generic;
using System.Linq;
using C7GameData;

namespace C7Engine;

public class DiplomaticVictory : IVictory {
	public string Header() => "Diplomatic";

	public VictoryStatus Evaluate(Player player, GameData gameData) {
		var ownsUnitedNations = player.cities
			.Any(c => c.GetBuildings().Any(b => b.building.CanTriggerDiplomaticVictoryVote));

		return new VictoryStatus {
			Player = player,
			OwnsUnitedNations = ownsUnitedNations
		};
	}

	public bool HasVictory(VictoryStatus status) {
		return false; // TODO: Diplomatic Victory
	}

	public IEnumerable<string[]> GenerateStatusRows(VictoryStatus status, List<VictoryStatus> rivalStatuses) {
		yield return UnitedNationsPrint(status, rivalStatuses);
	}

	private string[] UnitedNationsPrint(VictoryStatus status, List<VictoryStatus> rivalStatuses) {
		var ownsUnitedNations = rivalStatuses.FirstOrDefault(x => x.OwnsUnitedNations);

		Player owner = ownsUnitedNations?.Player ?? (status.OwnsUnitedNations ? status.Player : null);

		string builtBy = owner == null ? "" : $"\n{owner.civilization.name}";
		string builtByPlaceholder = owner == null ? "No one" : "";

		return [
			"Elected as leader",
			"",
			"",
			"",
			$"The United Nations built by:{builtBy}",
			builtByPlaceholder
		];
	}
}
