using C7GameData;

namespace C7Engine;

/// <summary>
/// Civ3 Manual - Raging:
/// You asked for it! The world is full of barbarians,and they appear in large numbers.
/// 
/// Note: Implementation is not based on known Civ3 AI logic.
/// </summary>
internal class RagingStrategy : BaseStrategy {
	protected override bool DecideToEngage(Player player, MapUnit unit, Orientation orientation) {
		return true;
	}

	protected override bool DecideToExplore(Player player, MapUnit unit, Orientation orientation) {
		return GameData.rng.Next(100) < 50;
	}
}
