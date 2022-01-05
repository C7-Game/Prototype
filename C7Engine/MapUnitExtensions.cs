namespace C7Engine
{

using C7GameData;

public static class MapUnitExtensions {
	public static void fortify(this MapUnit unit)
	{
		unit.facingDirection = TileDirection.SOUTHEAST;
		unit.isFortified = true;

		// Must send message to UI. Can't call animTracker directly since it doesn't belong to the engine. This is a race condition.
		// EngineStorage.animTracker.startAnimation(unit, MapUnit.AnimatedAction.FORTIFY, null);
	}
}

}
