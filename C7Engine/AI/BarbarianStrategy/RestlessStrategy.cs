using C7GameData;

namespace C7Engine;

/// <summary>
/// Civ3 Manual -  Restless:
/// Barbarians appear in moderate up to significant numbers, at shorter intervals than at lower levels.
/// 
/// Note: Implementation is not based on known Civ3 AI logic.
/// </summary>
internal class RestlessStrategy : BaseStrategy {
	protected override bool DecideToEngage(Player player, MapUnit unit, Orientation orientation) {
		return GameData.rng.Next(100) < 75;
	}

	protected override bool DecideToExplore(Player player, MapUnit unit, Orientation orientation) {
		return GameData.rng.Next(100) < 75;
	}
}
