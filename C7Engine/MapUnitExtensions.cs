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


	public static void move(this MapUnit unit, TileDirection dir)
	{
		(int dx, int dy) = dir.toCoordDiff();
		var newLoc = EngineStorage.gameData.map.tileAt(dx + unit.location.xCoordinate, dy + unit.location.yCoordinate);
		if ((newLoc != null) && (unit.movementPointsRemaining > 0)) {
			if (!unit.location.unitsOnTile.Remove(unit))
				throw new System.Exception("Failed to remove unit from tile it's supposed to be on");
			newLoc.unitsOnTile.Add(unit);
			unit.location = newLoc;
			unit.facingDirection = dir;
			unit.movementPointsRemaining -= newLoc.overlayTerrainType.movementCost;
			unit.isFortified = false;
			new MsgStartAnimation(unit, MapUnit.AnimatedAction.RUN, null).send();
		}
	}

	public static void skipTurn(this MapUnit unit)
	{
		/**
		* I'd like to enhance this so it's like Civ4, where the skip turn action takes the unit out of the rotation, but you can change your
		* mind if need be.  But for now it'll be like Civ3, where you're out of luck if you realize that unit was needed for something; that
		* also simplifies things here.
		**/
		unit.movementPointsRemaining = 0;
	}

	public static void disband(this MapUnit unit)
	{
		// Set unit's hit points to zero to indicate that it's no longer alive. Ultimately we may not want to do this. I'm only doing it right
		// now since this way all the UI needs to do to check if the selected unit has been destroyed is to check its hit points.
		unit.hitPointsRemaining = 0;

		// EngineStorage.animTracker.endAnimation(unit, false);   TODO: Must send message instead of call directly
		unit.location.unitsOnTile.Remove(unit);
		EngineStorage.gameData.mapUnits.Remove(unit);
	}

	public static void buildCity(this MapUnit unit, string cityName)
	{
		// TODO: Need to check somewhere that this unit is allowed to build a city on its current tile. Either do that here or in every caller
		// (probably best to just do it here).
		CityInteractions.BuildCity(unit.location.xCoordinate, unit.location.yCoordinate, unit.owner.guid, cityName);

		// TODO: Should directly delete the unit instead of disbanding it. Disbanding in a city will eventually award shields, which we
		// obviously don't want to do here.
		unit.disband();
	}
}

}
