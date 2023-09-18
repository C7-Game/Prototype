using Serilog;

namespace C7Engine {
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using Pathing;
	using C7GameData;

	//We should document why we're putting things in the extensions methods.  We discussed it a month or so ago, but I forget why at this point.
	//Coming from an OO background, I'm wondering why these aren't on the MapUnit class... data access?  Modding?  Some other benefit?
	public static class MapUnitExtensions {

		private static ILogger log = Log.ForContext<MapUnit>();

		public static void animate(this MapUnit unit, MapUnit.AnimatedAction action, bool wait, AnimationEnding ending = AnimationEnding.Stop) {
			if (EngineStorage.animationsEnabled) {
				new MsgStartUnitAnimation(unit, action, wait ? EngineStorage.uiEvent : null, ending).send();
				if (wait) {
					EngineStorage.gameDataMutex.ReleaseMutex();
					EngineStorage.uiEvent.WaitOne();
					EngineStorage.gameDataMutex.WaitOne();
				}
			}
		}

		public static void fortify(this MapUnit unit) {
			unit.facingDirection = TileDirection.SOUTHEAST;
			unit.isFortified = true;
			unit.animate(MapUnit.AnimatedAction.FORTIFY, false);
		}

		public static void wake(this MapUnit unit) {
			unit.isFortified = false;
		}

		public static IEnumerable<StrengthBonus> ListStrengthBonusesVersus(this MapUnit unit, MapUnit opponent, CombatRole role, TileDirection? attackDirection) {
			GameData gD = EngineStorage.gameData;

			if (role.Defending()) {
				if (unit.isFortified)
					yield return gD.fortificationBonus;

				yield return unit.location.overlayTerrainType.defenseBonus;

				if ((!role.Bombarding()) && (attackDirection is TileDirection dir) && unit.location.HasRiverCrossing(dir.reversed()))
					yield return gD.riverCrossingBonus;

				// TODO: Bonus should vary depending on city level. First we must load the thresholds for level 2/3 into the scenario data.
				if (unit.location.cityAtTile != null)
					yield return gD.cityLevel2DefenseBonus;
			}
		}

		public static double StrengthVersus(this MapUnit unit, MapUnit opponent, CombatRole role, TileDirection? attackDirection) {
			return unit.unitType.BaseStrength(role) * StrengthBonus.ListToMultiplier(unit.ListStrengthBonusesVersus(opponent, role, attackDirection));
		}

		public static bool CanDefendAgainst(this MapUnit unit, MapUnit attacker) {
			//Basically, unit type must match.  Sea/air units in a city/airfield can't defend against land units.
			//Land units on a boat or planes on a carrier can't defend against boats.  Anti-air is another category that should be checked before the direct combat.
			//Potential future hybrid units that have multiple categories (e.g. amphibious vehicles) may contain more than one category.
			if (attacker.unitType.categories.Contains("Land") && !unit.unitType.categories.Contains("Land")) {
				return false;
			}
			if (attacker.unitType.categories.Contains("Sea") && !unit.unitType.categories.Contains("Sea")) {
				return false;
			}
			if (attacker.unitType.categories.Contains("Air") && !unit.unitType.categories.Contains("Air")) {
				return false;
			}
			return true;
		}

		// Answers the question: if "opponent" is attacking the tile that this unit is standing on, does this unit defend instead of "otherDefender"?
		// Note that otherDefender does not necessarily belong to the same civ as this unit. Under standard Civ 3 rules you can't have units belonging
		// to two different civs on the same tile, but we don't want to assume that. In that case, whoever is an enemy of "opponent" should get
		// priority. Otherwise it's just whoever is stronger on defense.
		public static bool HasPriorityAsDefender(this MapUnit unit, MapUnit otherDefender, MapUnit opponent) {
			Player opponentPlayer = opponent.owner;
			bool weAreEnemy           = (opponentPlayer != null) ? ! opponentPlayer.IsAtPeaceWith(unit.owner)          : false;
			bool otherDefenderIsEnemy = (opponentPlayer != null) ? ! opponentPlayer.IsAtPeaceWith(otherDefender.owner) : false;
			if (weAreEnemy && !otherDefenderIsEnemy)
				return true;
			else if (otherDefenderIsEnemy && !weAreEnemy)
				return false;
			else {
				double ourTotalStrength   = unit         .StrengthVersus(opponent, CombatRole.Defense, null) * unit         .hitPointsRemaining,
				   theirTotalStrength = otherDefender.StrengthVersus(opponent, CombatRole.Defense, null) * otherDefender.hitPointsRemaining;
				return ourTotalStrength > theirTotalStrength;
			}
		}


		public static void RollToPromote(this MapUnit unit, MapUnit opponent, bool waitForAnimation) {
			double promotionChance = unit.experienceLevel.promotionChance;
			if (opponent.owner.isBarbarians)
				promotionChance /= 2.0;
			// TODO: Double promotionChance if unit is owned by a militaristic civ

			if (GameData.rng.NextDouble() < promotionChance) {
				ExperienceLevel nextLevel = EngineStorage.gameData.GetExperienceLevelAfter(unit.experienceLevel);
				if (nextLevel != null) {
					unit.experienceLevelKey = nextLevel.key;
					unit.experienceLevel = nextLevel;
					unit.hitPointsRemaining++;
					unit.animate(MapUnit.AnimatedAction.VICTORY, waitForAnimation);
				}
			}
		}

		public static double RetreatChance(this MapUnit unit, MapUnit opponent, bool isAttacking) {
			return ((unit.unitType.movement > 1) && (opponent.unitType.movement <= 1)) ? unit.experienceLevel.retreatChance : 0.0;
		}

		public static CombatResult fight(this MapUnit attacker, MapUnit defender) {
			// Rotate defender to face its attacker. We'll restore the original facing direction at the end of the battle.
			var defenderOriginalDirection = defender.facingDirection;
			defender.facingDirection = attacker.facingDirection.reversed();

			IEnumerable<StrengthBonus> attackBonuses  = attacker.ListStrengthBonusesVersus(defender, CombatRole.Attack , attacker.facingDirection),
								   defenseBonuses = defender.ListStrengthBonusesVersus(attacker, CombatRole.Defense, attacker.facingDirection);

			double attackerStrength = attacker.unitType.attack  * StrengthBonus.ListToMultiplier(attackBonuses),
			   defenderStrength = defender.unitType.defense * StrengthBonus.ListToMultiplier(defenseBonuses);

			log.Information($"Combat log: {attacker.unitType.name} ({attackerStrength}) attacking {defender.unitType.name} ({defenderStrength})");
			log.Information($"\tAttacker: {attacker.unitType.name}, base strength {attacker.unitType.BaseStrength(CombatRole.Attack)}");
			foreach (StrengthBonus bonus in attackBonuses)
				log.Information($"\t\t+{100.0 * bonus.amount}%\t{bonus.description}");
			log.Information($"\tDefender: {defender.unitType.name}, base strength {defender.unitType.BaseStrength(CombatRole.Defense)}");
			foreach (StrengthBonus bonus in defenseBonuses)
				log.Information($"\t\t+{100.0 * bonus.amount}%\t{bonus.description}");

			CombatResult result = CombatResult.Impossible;

			double attackerOdds = attackerStrength / (attackerStrength + defenderStrength);
			if (Double.IsNaN(attackerOdds))
				return result;

			// Defensive bombard
			MapUnit defensiveBombarder = MapUnit.NONE;
			double defensiveBombarderStrength = 0.0;
			foreach (MapUnit candidate in defender.location.unitsOnTile.Where(u => u != defender && !u.owner.IsAtPeaceWith(attacker.owner) && u.defensiveBombardsRemaining > 0)) {
				double strength = candidate.StrengthVersus(attacker, CombatRole.DefensiveBombard, attacker.facingDirection.reversed());
				if (strength > defensiveBombarderStrength) {
					defensiveBombarder = candidate;
					defensiveBombarderStrength = strength;
				}
			}
			// In the original game, defensive bombard does not trigger against attackers with 1 HP. See:
			// https://github.com/C7-Game/Prototype/pull/250#discussion_r893051111
			if (defensiveBombarder != MapUnit.NONE && attacker.hitPointsRemaining > 1) {
				var dBOriginalDirection = defensiveBombarder.facingDirection;
				defensiveBombarder.facingDirection = defender.facingDirection;

				defensiveBombarder.animate(MapUnit.AnimatedAction.ATTACK1, true);

				// dADB = defense Against Defensive Bombard
				double dADB = attacker.StrengthVersus(defensiveBombarder, CombatRole.DefensiveBombardDefense, defensiveBombarder.facingDirection);
				if (GameData.rng.NextDouble() < defensiveBombarderStrength / (defensiveBombarderStrength + dADB))
					attacker.hitPointsRemaining -= 1;

				defensiveBombarder.defensiveBombardsRemaining -= 1;
				defensiveBombarder.facingDirection = dBOriginalDirection;
			}

			bool defenderEligibleToRetreat = defender.hitPointsRemaining > 1 && ! defender.location.HasCity;

			// Do combat rounds
			while (true) {
				defender.animate(MapUnit.AnimatedAction.ATTACK1, false);
				attacker.animate(MapUnit.AnimatedAction.ATTACK1, true);
				if (GameData.rng.NextDouble() < attackerOdds) {
					if (defenderEligibleToRetreat &&
						defender.hitPointsRemaining == 1 &&
						GameData.rng.NextDouble() < defender.RetreatChance(attacker, false)) {
						// TODO: Defender retreat behavior requires some more work. There's an issue for it here:
						// https://github.com/C7-Game/Prototype/issues/274
						Tile retreatDestination = defender.location.neighbors[attacker.facingDirection];
						if ((retreatDestination != Tile.NONE) && defender.CanEnterTile(retreatDestination, false)) {
							defender.move(attacker.facingDirection, true);
							result = CombatResult.DefenderRetreated;
							break;
						}
					}
					defender.hitPointsRemaining -= 1;
					if (defender.hitPointsRemaining <= 0) {
						result = CombatResult.DefenderKilled;
						break;
					}
				} else {
					if (attacker.hitPointsRemaining == 1 &&
						GameData.rng.NextDouble() < attacker.RetreatChance(defender, true)) {
						result = CombatResult.AttackerRetreated;
						break;
					}
					attacker.hitPointsRemaining -= 1;
					if (attacker.hitPointsRemaining <= 0) {
						result = CombatResult.AttackerKilled;
						break;
					}
				}
			}

			if ((result == CombatResult.AttackerKilled) || (result == CombatResult.DefenderKilled)) {
				var (dead, alive) = (result == CombatResult.AttackerKilled) ? (attacker, defender) : (defender, attacker);
				alive.RollToPromote(dead, false);
				dead.animate(MapUnit.AnimatedAction.DEATH, true);
				dead.disband();
			}

			if (result.DefenderWon())
				defender.facingDirection = defenderOriginalDirection;

			return result;
		}

		public static void bombard(this MapUnit unit, Tile tile) {
			MapUnit target = tile.FindTopDefender(unit);
			if ((unit.unitType.bombard == 0) || (target == MapUnit.NONE))
				return; // Do nothing if we don't have a unit to attack. TODO: Attack city or tile improv if possible

			var unitOriginalOrientation = unit.facingDirection;
			unit.facingDirection = unit.location.directionTo(tile);

			double bombardStrength  = unit  .StrengthVersus(target, CombatRole.Bombard       , unit.facingDirection);
			double defenderStrength = target.StrengthVersus(unit  , CombatRole.BombardDefense, unit.facingDirection);
			double attackerOdds = bombardStrength / (bombardStrength + defenderStrength);
			if (Double.IsNaN(attackerOdds))
				return;

			unit.animate(MapUnit.AnimatedAction.ATTACK1, true);
			unit.movementPoints.onUnitMove(1);
			if (GameData.rng.NextDouble() < attackerOdds) {
				target.hitPointsRemaining -= 1;
				tile.Animate(AnimatedEffect.Hit3, false);
			} else
				tile.Animate(AnimatedEffect.Miss, false);

			if (target.hitPointsRemaining <= 0) {
				unit.RollToPromote(target, false);
				target.animate(MapUnit.AnimatedAction.DEATH, true);
				target.disband();
			}

			unit.facingDirection = unitOriginalOrientation;
		}

		public static int HealRateAt(this MapUnit unit, Tile location) {
			GameData gD = EngineStorage.gameData;
			City city = location.cityAtTile;
			bool inFriendlyCity = (city != null) && (city != City.NONE) && unit.owner.IsAtPeaceWith(city.owner);
			if (inFriendlyCity)
				return gD.healRateInCity;
			if (unit.unitType.categories.Contains("Sea"))
				return 0;
			return gD.healRateInNeutralField;
			// TODO: Consider friendly/neutral/enemy territory once that's implemented, barracks, the Red Cross
		}

		public static void OnBeginTurn(this MapUnit unit) {
			int maxMP = unit.unitType.movement;
			if (unit.movementPoints.remaining >= maxMP) {
				int maxHP = unit.maxHitPoints;
				if (unit.hitPointsRemaining < maxHP)
					unit.hitPointsRemaining += unit.HealRateAt(unit.location);
				if (unit.hitPointsRemaining > maxHP)
					unit.hitPointsRemaining = maxHP;
			}
			unit.movementPoints.reset(maxMP);
			unit.defensiveBombardsRemaining = 1;
		}

		public static void OnEnterTile(this MapUnit unit, Tile tile) {
			//Add to player knowledge of tiles
			unit.owner.tileKnowledge.AddTilesToKnown(tile);

			// Disperse barb camp
			if (tile.hasBarbarianCamp && (!unit.owner.isBarbarians)) {
				tile.DisbandNonDefendingUnits();
				EngineStorage.gameData.map.barbarianCamps.Remove(tile);
				tile.hasBarbarianCamp = false;
			}

			// Destroy enemy city on tile
			if (tile.HasCity && !unit.owner.IsAtPeaceWith(tile.cityAtTile.owner)) {
				CityInteractions.DestroyCity(tile.xCoordinate, tile.yCoordinate);
			}
		}

		public static bool CanEnterTile(this MapUnit unit, Tile tile, bool allowCombat) {
			// Keep land units on land and sea units on water
			if (unit.unitType.categories.Contains("Sea") && tile.IsLand()) {
				if (tile.HasCity && tile.cityAtTile.owner == unit.owner) {
					return true;
				}
				return false;
			} else if (unit.unitType.categories.Contains("Land") && !tile.IsLand())
				return false;

			// Check for units belonging to other civs
			foreach (MapUnit other in tile.unitsOnTile)
				if (other.owner != unit.owner) {
					if (!other.owner.IsAtPeaceWith(unit.owner))
						return allowCombat;
					else
						return false;
				}

			return true;
		}

		/// <summary>
		/// Moves the unit in the given direction
		/// </summary>
		/// <param name="unit"></param>
		/// <param name="dir">Which direction to move, e.g. northeast, west, etc.</param>
		/// <param name="wait">Whether the method should wait to return until animations complete</param>
		/// <returns>True if the unit is alive after the movement, false otherwise</returns>
		/// <exception cref="Exception"></exception>
		public static bool move(this MapUnit unit, TileDirection dir, bool wait = false) {
			(int dx, int dy) = dir.toCoordDiff();
			Tile newLoc = EngineStorage.gameData.map.tileAt(dx + unit.location.xCoordinate, dy + unit.location.yCoordinate);
			if ((newLoc != Tile.NONE) && unit.CanEnterTile(newLoc, true) && (unit.movementPoints.canMove)) {
				unit.facingDirection = dir;
				unit.wake();

				// Trigger combat if the tile we're moving into has an enemy unit. Or if this unit can't fight, do nothing.
				MapUnit defender = newLoc.FindTopDefender(unit);
				if (defender != MapUnit.NONE && !unit.owner.IsAtPeaceWith(defender.owner)) {
					if (unit.unitType.attack > 0) {
						CombatResult combatResult = unit.fight(defender);
						// If we were killed then of course there's nothing more to do. If the combat couldn't happen for whatever
						// reason, just give up on trying to move.
						if (combatResult == CombatResult.AttackerKilled) {
							return false;
						}
						if (combatResult == CombatResult.Impossible) {
							return true;
						}

						// If the enemy was defeated, check if there is another enemy on the tile. If so we can't complete the move
						// but still pay one movement point for the combat.
						else if (combatResult == CombatResult.DefenderKilled || combatResult == CombatResult.DefenderRetreated) {
							if (!unit.CanEnterTile(newLoc, false)) {
								unit.movementPoints.onUnitMove(1);
								return true;
							}

							// Similarly if we retreated, pay one MP for the combat but don't move.
						} else if (combatResult == CombatResult.AttackerRetreated) {
							unit.movementPoints.onUnitMove(1);
							return true;
						}
					} else if (unit.unitType.bombard > 0) {
						unit.bombard(newLoc);
						return true;
					} else {
						return true;
					}
				}

				float movementCost = getMovementCost(unit.location, dir, newLoc);
				if (!unit.location.unitsOnTile.Remove(unit))
					throw new System.Exception("Failed to remove unit from tile it's supposed to be on");
				unit.OnEnterTile(newLoc);
				newLoc.unitsOnTile.Add(unit);
				unit.location = newLoc;
				unit.movementPoints.onUnitMove(movementCost);
				unit.animate(MapUnit.AnimatedAction.RUN, wait);
			}
			return true;
		}

		public static float getMovementCost(Tile from, TileDirection dir, Tile newLocation) {
			if (from.HasRiverCrossing(dir)) return newLocation.MovementCost();
			if (from.overlays.railroad && newLocation.overlays.railroad) return 0;
			if ((from.overlays.railroad || from.overlays.road) && newLocation.overlays.road) return 1.0f / 3;
			return newLocation.MovementCost();
		}

		public static void moveAlongPath(this MapUnit unit) {
			while (unit.movementPoints.canMove && unit.path?.PathLength() > 0) {
				TileDirection dir = unit.location.directionTo(unit.path.Next());
				unit.move(dir, true); //TODO: don't wait on last move animation?
			}
		}

		public static void setUnitPath(this MapUnit unit, Tile dest) {
			unit.path = PathingAlgorithmChooser.GetAlgorithm().PathFrom(unit.location, dest);
			if (unit.path == TilePath.NONE) {
				log.Warning("Cannot move unit to " + dest + ", path is NONE!");
			}
			unit.moveAlongPath();
		}

		public static void skipTurn(this MapUnit unit) {
			unit.movementPoints.skipTurn();
		}

		public static void disband(this MapUnit unit) {
			GameData gameData = EngineStorage.gameData;

			// Set unit's hit points to zero to indicate that it's no longer alive. Ultimately we may not want to do this. I'm only doing it right
			// now since this way all the UI needs to do to check if the selected unit has been destroyed is to check its hit points.
			unit.hitPointsRemaining = 0;

			// EngineStorage.animTracker.endAnimation(unit, false);   TODO: Must send message instead of call directly
			unit.location.unitsOnTile.Remove(unit);
			gameData.mapUnits.Remove(unit);
			foreach (Player player in gameData.players) {
				if (player.units.Contains(unit)) {
					player.units.Remove(unit);
				}
			}
		}

		public static bool canBuildCity(this MapUnit unit) {
			if (!unit.unitType.actions.Contains(C7Action.UnitBuildCity)) {
				return false;
			}
			if (unit.location.HasCity || !unit.location.IsAllowCities()) {
				return false;
			}
			return unit.location.neighbors.Values.All(tile => !tile.HasCity);
		}

		public static void buildCity(this MapUnit unit, string cityName) {
			if (!unit.canBuildCity()) {
				log.Warning($"can't build city at {unit.location}");
				return;
			}

			unit.animate(MapUnit.AnimatedAction.BUILD, true);

			// TODO: Need to check somewhere that this unit is allowed to build a city on its current tile. Either do that here or in every caller
			// (probably best to just do it here).
			CityInteractions.BuildCity(unit.location.xCoordinate, unit.location.yCoordinate, unit.owner.id, cityName);

			// TODO: Should directly delete the unit instead of disbanding it. Disbanding in a city will eventually award shields, which we
			// obviously don't want to do here.
			unit.disband();
		}

		public static bool canBuildRoad(this MapUnit unit) {
			return unit.unitType.actions.Contains(C7Action.UnitBuildRoad) && unit.location.IsLand() && !unit.location.overlays.road;
		}

		public static void buildRoad(this MapUnit unit) {
			if (!unit.canBuildRoad()) {
				log.Warning($"can't build road by {unit}");
				return;
			}

			// TODO add animation and long process of building
			unit.location.overlays.road = true;
			unit.movementPoints.onConsumeAll();
		}

	}
}
