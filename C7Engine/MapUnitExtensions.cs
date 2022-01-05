namespace C7Engine
{

using C7GameData;

public static class MapUnitExtensions {
	public static void fortify(this MapUnit unit)
	{
		unit.facingDirection = TileDirection.SOUTHEAST;
		unit.isFortified = true;
		new MsgStartAnimation(unit, MapUnit.AnimatedAction.FORTIFY, null).send();
	}
}

}
