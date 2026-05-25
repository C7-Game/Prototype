using C7GameData;

namespace C7Engine;

/// <summary>
/// Civ3 Manual - Sedentary:
/// Barbarians are restricted to their encampments. The surrounding terrain is free of their mischief.
///
/// Note: Implementation is not based on known Civ3 AI logic.
/// </summary>
internal class SedentaryStrategy : BaseStrategy {
	protected override bool DecideToEngage(Player player, MapUnit unit, Orientation orientation) {
		return false; // TODO: attack units next to camp?
	}

	protected override bool DecideToExplore(Player player, MapUnit unit, Orientation orientation) {
		return false;
	}
}
