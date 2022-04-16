namespace C7Engine
{

using System;
using System.Collections.Generic;
using Pathing;
using C7GameData;

//We should document why we're putting things in the extensions methods.  We discussed it a month or so ago, but I forget why at this point.
//Coming from an OO background, I'm wondering why these aren't on the MapUnit class... data access?  Modding?  Some other benefit?
public static class MapUnitExtensions {
	public static void animate(this MapUnit unit, MapUnit.AnimatedAction action, bool wait, AnimationEnding ending = AnimationEnding.Stop)
	{
		new MsgStartUnitAnimation(unit, action, wait ? EngineStorage.uiEvent : null, ending).send();
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

	public static void wake(this MapUnit unit)
	{
		unit.isFortified = false;
	}

	public static IEnumerable<StrengthBonus> ListStrengthBonusesVersus(this MapUnit unit, MapUnit opponent, bool attacking, bool bombard, TileDirection? attackDirection)
	{
		if (! attacking) { // Defending against attack from opponent
			if (unit.isFortified)
				yield return new StrengthBonus { description = "Fortification", amount = 0.25 };
		}
	}

	public static double AttackStrengthVersus(this MapUnit unit, MapUnit opponent, bool bombard, TileDirection? attackDirection)
	{
		double multiplier = StrengthBonus.ListToMultiplier(unit.ListStrengthBonusesVersus(opponent, true, bombard, attackDirection));
		return multiplier * (bombard ? unit.unitType.bombard : unit.unitType.attack);
	}

	public static double DefenseStrengthVersus(this MapUnit unit, MapUnit opponent, bool bombard, TileDirection? attackDirection)
	{
		return unit.unitType.defense * StrengthBonus.ListToMultiplier(unit.ListStrengthBonusesVersus(opponent, false, bombard, attackDirection));
	}

	// Answers the question: if "opponent" is attacking the tile that this unit is standing on, does this unit defend instead of "otherDefender"?
	// Note that otherDefender does not necessarily belong to the same civ as this unit. Under standard Civ 3 rules you can't have units belonging
	// to two different civs on the same tile, but we don't want to assume that. In that case, whoever is an enemy of "opponent" should get
	// priority. Otherwise it's just whoever is stronger on defense.
	public static bool HasPriorityAsDefender(this MapUnit unit, MapUnit otherDefender, MapUnit opponent)
	{
		Player opponentPlayer = opponent.owner;
		bool weAreEnemy           = (opponentPlayer != null) ? ! opponentPlayer.IsAtPeaceWith(unit.owner)          : false;
		bool otherDefenderIsEnemy = (opponentPlayer != null) ? ! opponentPlayer.IsAtPeaceWith(otherDefender.owner) : false;
		if (weAreEnemy && ! otherDefenderIsEnemy)
			return true;
		else if (otherDefenderIsEnemy && ! weAreEnemy)
			return false;
		else {
			double ourTotalStrength   = unit         .DefenseStrengthVersus(opponent, false, null) * unit         .hitPointsRemaining,
			       theirTotalStrength = otherDefender.DefenseStrengthVersus(opponent, false, null) * otherDefender.hitPointsRemaining;
			return ourTotalStrength > theirTotalStrength;
		}
	}


	public static void RollToPromote(this MapUnit unit, bool wasAttacking, bool waitForAnimation)
	{
		C7RulesFormat rules = EngineStorage.rules;
		double promotionOdds = wasAttacking ? rules.promotionChanceAfterAttacking : rules.promotionChanceAfterDefending;
		if (EngineStorage.gameData.rng.NextDouble() < promotionOdds) {
			ExperienceLevel nextLevel = rules.GetExperienceLevelAfter(unit.experienceLevel);
			if (nextLevel != null) {
				unit.experienceLevelKey = nextLevel.key;
				unit.experienceLevel = nextLevel;
				unit.hitPointsRemaining++;
				unit.animate(MapUnit.AnimatedAction.VICTORY, waitForAnimation);
			}
		}
	}

	public static bool fight(this MapUnit unit, MapUnit defender)
	{
		// Rotate defender to face its attacker. We'll restore the original facing direction at the end of the battle.
		var defenderOriginalDirection = defender.facingDirection;
		defender.facingDirection = unit.facingDirection.reversed();

		double attackerStrength = unit    .AttackStrengthVersus (defender, false, unit.facingDirection),
		       defenderStrength = defender.DefenseStrengthVersus(unit    , false, unit.facingDirection);

		double attackerOdds = attackerStrength / (attackerStrength + defenderStrength);
		if (Double.IsNaN(attackerOdds))
			return false;

		// Do combat rounds
		while ((unit.hitPointsRemaining > 0) && (defender.hitPointsRemaining > 0)) {
			defender.animate(MapUnit.AnimatedAction.ATTACK1, false);
			unit    .animate(MapUnit.AnimatedAction.ATTACK1, true );
			if (EngineStorage.gameData.rng.NextDouble() < attackerOdds)
				defender.hitPointsRemaining -= 1;
			else
				unit.hitPointsRemaining -= 1;
		}

		MapUnit loser = (defender.hitPointsRemaining <= 0) ? defender : unit,
			winner = (defender == loser) ? unit : defender;

		winner.RollToPromote(winner == unit, false);

		// Play death animation
		loser.animate(MapUnit.AnimatedAction.DEATH, true);
		loser.disband();

		if (defender != loser)
			defender.facingDirection = defenderOriginalDirection;

		return unit != loser;
	}

	public static void bombard(this MapUnit unit, Tile tile)
	{
		MapUnit target = tile.FindTopDefender(unit);
		if ((unit.unitType.bombard == 0) || (target == MapUnit.NONE))
			return; // Do nothing if we don't have a unit to attack. TODO: Attack city or tile improv if possible

		var unitOriginalOrientation = unit.facingDirection;
		unit.facingDirection = unit.location.directionTo(tile);

		double bombardStrength  = unit  .AttackStrengthVersus (target, true, unit.facingDirection);
		double defenderStrength = target.DefenseStrengthVersus(unit  , true, unit.facingDirection);
		double attackerOdds = bombardStrength / (bombardStrength + defenderStrength);
		if (Double.IsNaN(attackerOdds))
			return;

		unit.animate(MapUnit.AnimatedAction.ATTACK1, true);
		unit.movementPointsRemaining -= 1;
		if (EngineStorage.gameData.rng.NextDouble() < attackerOdds) {
			target.hitPointsRemaining -= 1;
			new MsgStartEffectAnimation(tile, AnimatedEffect.Hit3, null, AnimationEnding.Stop).send();
		} else
			new MsgStartEffectAnimation(tile, AnimatedEffect.Miss, null, AnimationEnding.Stop).send();

		if (target.hitPointsRemaining <= 0) {
			unit.RollToPromote(true, false);
			target.animate(MapUnit.AnimatedAction.DEATH, true);
			target.disband();
		}

		unit.facingDirection = unitOriginalOrientation;
	}

	public static void OnEnterTile(this MapUnit unit, Tile tile)
	{
		// Disperse barb camp
		if (tile.hasBarbarianCamp && (!unit.owner.isBarbarians)) {
			EngineStorage.gameData.map.barbarianCamps.Remove(tile);
			tile.hasBarbarianCamp = false;
		}

		// Destroy enemy city on tile
		if ((tile.cityAtTile != null) && (!unit.owner.IsAtPeaceWith(tile.cityAtTile.owner))) {
			tile.cityAtTile.owner.cities.Remove(tile.cityAtTile);
			EngineStorage.gameData.cities.Remove(tile.cityAtTile);
			tile.cityAtTile = null;
		}
	}

	public static void move(this MapUnit unit, TileDirection dir, bool wait = false)
	{
		(int dx, int dy) = dir.toCoordDiff();
		var newLoc = EngineStorage.gameData.map.tileAt(dx + unit.location.xCoordinate, dy + unit.location.yCoordinate);
		if ((newLoc != Tile.NONE) && newLoc.IsLand() && (unit.movementPointsRemaining > 0)) {
			unit.facingDirection = dir;
			unit.wake();

			// Trigger combat if the tile we're moving into has an enemy unit. Or if this unit can't fight, do nothing.
			MapUnit defender = newLoc.FindTopDefender(unit);
			if ((defender != MapUnit.NONE) && (!unit.owner.IsAtPeaceWith(defender.owner))) {
				if (unit.unitType.attack > 0) {
					bool unitWonCombat = unit.fight(defender);
					if (!unitWonCombat)
						return;

					// If there are still more enemy units on the destination tile we can't actually move into it
					defender = newLoc.FindTopDefender(unit);
					if ((defender != MapUnit.NONE) && (! unit.owner.IsAtPeaceWith(defender.owner))) {
						unit.movementPointsRemaining -= 1;
						return;
					}
				} else if (unit.unitType.bombard > 0) {
					unit.bombard(newLoc);
					return;
				} else {
					return;
				}
			}

			if (!unit.location.unitsOnTile.Remove(unit))
				throw new System.Exception("Failed to remove unit from tile it's supposed to be on");
			newLoc.unitsOnTile.Add(unit);
			unit.location = newLoc;
			unit.movementPointsRemaining -= newLoc.overlayTerrainType.movementCost;
			unit.OnEnterTile(newLoc);
			unit.animate(MapUnit.AnimatedAction.RUN, wait);
		}
	}

	public static void moveAlongPath(this MapUnit unit)
	{
		while (unit.movementPointsRemaining > 0 && unit.path?.PathLength() > 0) {
			var dir = unit.location.directionTo(unit.path.Next());
			unit.move(dir, true); //TODO: don't wait on last move animation?
		}
	}

	public static void setUnitPath(this MapUnit unit, Tile dest)
	{
		unit.path = PathingAlgorithmChooser.GetAlgorithm().PathFrom(unit.location, dest);
		if (unit.path == TilePath.NONE) {
			System.Console.WriteLine("Cannot move unit to " + dest + ", path is NONE!");
		}
		unit.moveAlongPath();
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
		GameData gameData = EngineStorage.gameData;

		// Set unit's hit points to zero to indicate that it's no longer alive. Ultimately we may not want to do this. I'm only doing it right
		// now since this way all the UI needs to do to check if the selected unit has been destroyed is to check its hit points.
		unit.hitPointsRemaining = 0;

		// EngineStorage.animTracker.endAnimation(unit, false);   TODO: Must send message instead of call directly
		unit.location.unitsOnTile.Remove(unit);
		gameData.mapUnits.Remove(unit);
		foreach(Player player in gameData.players)
		{
			if (player.units.Contains(unit)) {
				player.units.Remove(unit);
			}
		}
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

	public static bool canTraverseTile(this MapUnit unit, Tile t) {
		//TODO: Unit prototypes should have info about terrain classes (#148), and we shouldn't rely on names
		if (unit.unitType.name == "Galley" && !t.IsLand())
		{
			return true;
		}
		else if (t.IsLand())
		{
			return true;
		}
		return false;
	}
	}
}
