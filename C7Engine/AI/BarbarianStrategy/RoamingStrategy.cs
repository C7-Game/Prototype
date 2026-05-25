using C7GameData;

namespace C7Engine;

/// <summary>
/// Civ3 Manual - Roaming:
/// Barbarian settlements occasionally appear, but less frequently and in smaller numbers
/// than at higher levels.This is the standard level of barbarian activity.
///
/// Note: Implementation is not based on known Civ3 AI logic.
/// </summary>
internal class RoamingStrategy : BaseStrategy {
	protected override bool DecideToEngage(Player player, MapUnit unit, Orientation orientation) {
		return GameData.rng.Next(100) < 50;
	}

	protected override bool DecideToExplore(Player player, MapUnit unit, Orientation orientation) {
		return GameData.rng.Next(100) < 75;
	}
}
