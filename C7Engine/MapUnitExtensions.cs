namespace C7Engine
{

using C7GameData;

public static class MapUnitExtensions {
	public static void animate(this MapUnit unit, MapUnit.AnimatedAction action, bool wait)
	{
		new MsgStartAnimation(unit, action, wait ? EngineStorage.uiEvent : null).send();
		if (wait) {
			EngineStorage.gameDataMutex.ReleaseMutex();
			EngineStorage.uiEvent.WaitOne();
			EngineStorage.gameDataMutex.WaitOne();
		}
	}

	public static void fortify(this MapUnit unit)
	{
		unit.facingDirection = TileDirection.SOUTHEAST;
		unit.isFortified = true;
		unit.animate(MapUnit.AnimatedAction.FORTIFY, false);
	}

	public static bool fight(this MapUnit unit, MapUnit defender)
	{
		// Rotate defender to face its attacker. We'll restore the original facing direction at the end of the battle.
		var defenderOriginalDirection = defender.facingDirection;
		defender.facingDirection = unit.facingDirection.reversed();

		int attackerStrength = unit.unitType.attack;
		int defenderStrength = defender.unitType.defense;

		if (attackerStrength + defenderStrength == 0)
			return false;

		double attackerOdds = (double)attackerStrength / (attackerStrength + defenderStrength);

		// Do combat rounds
		while ((unit.hitPointsRemaining > 0) && (defender.hitPointsRemaining > 0)) {
			defender.animate(MapUnit.AnimatedAction.ATTACK1, false);
			unit    .animate(MapUnit.AnimatedAction.ATTACK1, true );
			if (EngineStorage.gameData.rng.NextDouble() < attackerOdds)
				defender.hitPointsRemaining -= 1;
			else
				unit.hitPointsRemaining -= 1;
		}

		// Play death animation
		MapUnit loser = (defender.hitPointsRemaining <= 0) ? defender : unit;
		loser.animate(MapUnit.AnimatedAction.DEATH, true);
		loser.disband();

		if (defender != loser)
			defender.facingDirection = defenderOriginalDirection;

		return unit != loser;
	}

	public static void move(this MapUnit unit, TileDirection dir)
	{
		(int dx, int dy) = dir.toCoordDiff();
		var newLoc = EngineStorage.gameData.map.tileAt(dx + unit.location.xCoordinate, dy + unit.location.yCoordinate);
		if ((newLoc != null) && (unit.movementPointsRemaining > 0)) {
			unit.facingDirection = dir;
			unit.isFortified = false;

			// Trigger combat if the tile we're moving into has an enemy unit
			MapUnit defender = newLoc.findTopDefender(unit);
			if ((defender != MapUnit.NONE) && (!unit.owner.IsAtPeaceWith(defender.owner))) {
				bool unitWonCombat = unit.fight(defender);
				if (! unitWonCombat)
					return;

				// If there are still more enemy units on the destination tile we can't actually move into it
				defender = newLoc.findTopDefender(unit);
				if ((defender != MapUnit.NONE) && (! unit.owner.IsAtPeaceWith(defender.owner))) {
					unit.movementPointsRemaining -= 1;
					return;
				}
			}

			if (!unit.location.unitsOnTile.Remove(unit))
				throw new System.Exception("Failed to remove unit from tile it's supposed to be on");
			newLoc.unitsOnTile.Add(unit);
			unit.location = newLoc;
			unit.movementPointsRemaining -= newLoc.overlayTerrainType.movementCost;
			unit.animate(MapUnit.AnimatedAction.RUN, false);
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
		unit.animate(MapUnit.AnimatedAction.BUILD, true);

		// TODO: Need to check somewhere that this unit is allowed to build a city on its current tile. Either do that here or in every caller
		// (probably best to just do it here).
		CityInteractions.BuildCity(unit.location.xCoordinate, unit.location.yCoordinate, unit.owner.guid, cityName);

		// TODO: Should directly delete the unit instead of disbanding it. Disbanding in a city will eventually award shields, which we
		// obviously don't want to do here.
		unit.disband();
	}
}

}
