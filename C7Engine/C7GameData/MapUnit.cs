namespace C7GameData {
	using Serilog;
	using System.Collections.Generic;
	using System.Text.Json.Serialization;
	using System.Linq;
	using C7Engine;
	using AIData;
	using System;
	using System.Threading.Tasks;

	/**
	 * A unit on the map.  Not to be confused with a unit prototype.
	 **/
	public class MapUnit {
		public ID id { get; internal set; }
		public string name { get; internal set; }
		public Civilization nationality { get; set; }
		public UnitPrototype unitType { get; set; }
		public Player owner { get; set; }
		public Tile previousLocation { get; internal set; }
		private Tile currentLocation;

		public Tile location {
			get => currentLocation;
			set {
				previousLocation = location;
				currentLocation = value;
			}
		}
		public TilePath path { get; set; }

		public string experienceLevelKey;
		[JsonIgnore]
		public ExperienceLevel experienceLevel { get; set; }

		public MovementPoints movementPoints = new MovementPoints();
		public int hitPointsRemaining { get; set; }
		public int maxHitPoints {
			get {
				return experienceLevel.baseHitPoints; // TODO: Include bonus HP from unit type
			}
		}
		public bool isFortified { get; set; }

		public bool isAutomated { get; set; }

		//sentry, etc. will come later.  For now, let's just have a couple things so we can cycle through units that aren't fortified.
		public int defensiveBombardsRemaining;

		public TileDirection facingDirection;

		public float WorkerProgressTowardsJob { get; set; }
		public Terraform WorkerJob { get; set; }

		public UnitAI currentAI;

		public MapUnit(ID id) {
			this.id = id;
		}

		internal MapUnit() { }

		public bool IsBusy() {
			return isFortified || (path != null && path.PathLength() > 0) || WorkerJob != null || isAutomated;
		}

		public bool IsLandUnit() {
			return this.unitType.categories.Contains("Land");
		}
		public bool IsWaterUnit() {
			return this.unitType.categories.Contains("Sea");
		}

		public bool CanDefendOnLand() {
			return IsLandUnit() && unitType.defense > 0;
		}

		public bool HasRank() {
			return this.unitType.attack > 0 || this.unitType.defense > 0;
		}

		public bool CanBeActive() {
			return !this.IsBusy() && this.movementPoints.canMove;
		}

		public bool IsCaptive() {
			return !string.Equals(this.nationality.name, this.owner.civilization.name, StringComparison.CurrentCultureIgnoreCase);
		}

		public override string ToString() {
			if (this != MapUnit.NONE) {
				return $"{this.owner} {this.GetDisplayName()} at [{this.location.XCoordinate}, {this.location.YCoordinate}] " +
					   $"with {this.movementPoints.getMixedNumber()} MP and {this.hitPointsRemaining} HP, id = {id}";
			} else {
				return "This is the NONE unit";
			}
		}

		public string GetDisplayName() {
			return this.IsCaptive() ? $"{this.name} ({this.nationality.name})" : this.name;
		}

		// TODO: best move this to lua at some point
		public string GetArtName() {
			if (this.unitType.art.mainArt.variations != null) {
				if (this.unitType.isWorker && this.IsCaptive()) {
					if (this.unitType.art.mainArt.variations.FirstOrDefault(s => s.Key.EndsWith("SLAVE")).Value != null)
						return this.unitType.art.mainArt.variations.First(s => s.Key.EndsWith("SLAVE")).Value;
				}

				if (this.unitType.art.mainArt.variations.TryGetValue($"{this.owner.eraCivilopediaName}", out var value))
					return value;

				//TODO: add military + science leader variation
			}

			return this.unitType.art.mainArt.defaultName;
		}

		public string Describe() {
			UnitPrototype type = this.unitType;
			string exp = this.HasRank() ? $"{this.experienceLevel.displayName}" : "";
			string hPDesc = ((type.attack > 0) || (type.defense > 0)) ? $" ({this.hitPointsRemaining}/{this.maxHitPoints})" : "";
			string displayName = this.IsCaptive() ? $" ({this.nationality.adjective}) {this.name}" : $" {this.name}";
			string attackDesc = (type.bombard > 0) ? $"{type.attack}({type.bombard})" : type.attack.ToString();
			string stats = $" ({attackDesc}.{type.defense}.{(EngineStorage.uiControllerID == this.owner.id ? $"{this.movementPoints.getMixedNumber()}/" : "")}{type.movement})";
			return $"{exp}{hPDesc}{displayName}{stats}".Trim();
		}

		// TODO: The contents of this enum are copy-pasted from UnitAction in Civ3UnitSprite.cs. We should unify these so we don't have two different
		// but virtually identical enums.
		public enum AnimatedAction {
			BLANK,
			DEFAULT,
			WALK,
			RUN,
			ATTACK1,
			ATTACK2,
			ATTACK3,
			DEFEND,
			DEATH,
			DEAD,
			FORTIFY,
			FORTIFYHOLD,
			FIDGET,
			VICTORY,
			TURNLEFT,
			TURNRIGHT,
			BUILD,
			ROAD,
			MINE,
			IRRIGATE,
			FORTRESS,
			CAPTURE,
			JUNGLE,
			FOREST,
			PLANT
		}

		public struct Appearance {
			public AnimatedAction action;
			public TileDirection direction;
			public float progress; // Varies 0 to 1
			public float offsetX, offsetY; // Offset is in grid cells from the unit's location
			public AnimationEnding ending;

			// When true, indicates that the animation is still playing (f.e. a unit is still running between tiles) so the UI shouldn't yet
			// autoselect another unit.
			public bool DeservesPlayerAttention() {
				// TODO: Special rules for different animations. We don't need to see workers do their thing but we do want to watch units
				// move. IMO we should also not show units fortifying even though I know the original game does.
				// This may also be the culprit behind why we can fortify a unit that is in motion.
				if (ending == AnimationEnding.Repeat) {
					return false;
				}
				return progress < 1.0;
			}
		}

		public static MapUnit NONE = new MapUnit(ID.None("unit"));

		private static ILogger log = Log.ForContext<MapUnit>();

		private const int JOB_PROGRESS_WORKER = 2;
		private const int JOB_PROGRESS_SLAVE = 1;

		private static int GetWorkerJobCost(Tile tile, Terraform workerJob) {
			// For the movement cost multiplier, see note 7
			// (https://apolyton.net/forum/civilization-series/civilization-iii/59815-civilization-iii-bic-file-format-2nd-thread?p=1362768#post1362768)
			// For example, clearing a forest has a cost of 4, but with a normal
			// worker that would take 2 turns. In order for the job to take the
			// expected 4 turns we need to multiply by the movement cost of the
			// terrain. This also makes roading hills/mountains more expensive.
			return workerJob.TurnsToComplete * tile.overlayTerrainType.movementCost;
		}

		public async Task animateAsync(MapUnit.AnimatedAction action, AnimationEnding ending = AnimationEnding.Stop) {
			if (EngineStorage.animationsEnabled && !EngineStorage.gameData.observerMode) {
				var msg = new MsgStartUnitAnimation(this, action, ending);
				msg.send();

				await EngineStorage.WaitForAnimationFinished(msg.animationId);
			}
			if (this.owner.isHuman)
				new MsgUnitMoved(this).send();
		}

		public void animate(MapUnit.AnimatedAction action, AnimationEnding ending = AnimationEnding.Stop) {
			_ = animateAsync(action, ending);
		}

		public void fortify() {
			facingDirection = TileDirection.SOUTHEAST;
			isFortified = true;
			animate(MapUnit.AnimatedAction.FORTIFY);
		}

		public void wake() {
			isFortified = false;
		}

		public IEnumerable<StrengthBonus> ListStrengthBonusesVersus(MapUnit opponent, CombatRole role, TileDirection? attackDirection) {
			GameData gD = EngineStorage.gameData;

			if (role.Defending()) {
				if (isFortified)
					yield return gD.fortificationBonus;

				yield return location.overlayTerrainType.defenseBonus;

				foreach (StrengthBonus sb in location.overlays.GetDefenseBonuses()) {
					yield return sb;
				}

				if ((!role.Bombarding()) && (attackDirection is TileDirection dir) && location.HasRiverCrossing(dir.reversed()))
					yield return gD.riverCrossingBonus;

				if (location.cityAtTile != null) {
					foreach (StrengthBonus sb in location.cityAtTile.GetDefenseBonuses()) {
						yield return sb;
					}
				}
			}
		}

		public double StrengthVersus(MapUnit opponent, CombatRole role, TileDirection? attackDirection) {
			return unitType.BaseStrength(role) * StrengthBonus.ListToMultiplier(ListStrengthBonusesVersus(opponent, role, attackDirection));
		}

		public bool CanDefendAgainst(MapUnit attacker) {
			//Basically, unit type must match.  Sea/air units in a city/airfield can't defend against land units.
			//Land units on a boat or planes on a carrier can't defend against boats.  Anti-air is another category that should be checked before the direct combat.
			//Potential future hybrid units that have multiple categories (e.g. amphibious vehicles) may contain more than one category.
			if (attacker.unitType.categories.Contains("Land") && !unitType.categories.Contains("Land")) {
				return false;
			}
			if (attacker.unitType.categories.Contains("Sea") && !unitType.categories.Contains("Sea")) {
				return false;
			}
			if (attacker.unitType.categories.Contains("Air") && !unitType.categories.Contains("Air")) {
				return false;
			}
			return true;
		}

		// Answers the question: if "opponent" is attacking the tile that this unit is standing on, does this unit defend instead of "otherDefender"?
		// Note that otherDefender does not necessarily belong to the same civ as this  Under standard Civ 3 rules you can't have units belonging
		// to two different civs on the same tile, but we don't want to assume that. In that case, whoever is an enemy of "opponent" should get
		// priority. Otherwise it's just whoever is stronger on defense.
		public bool HasPriorityAsDefender(MapUnit otherDefender, MapUnit opponent) {
			Player opponentPlayer = opponent.owner;
			bool weAreEnemy           = !opponentPlayer?.IsAtPeaceWith(owner) ?? false;
			bool otherDefenderIsEnemy = !opponentPlayer?.IsAtPeaceWith(otherDefender.owner) ?? false;

			if (weAreEnemy && !otherDefenderIsEnemy)
				return true;
			if (otherDefenderIsEnemy && !weAreEnemy)
				return false;

			double ourTotalStrength = StrengthVersus(opponent, CombatRole.Defense, null) * hitPointsRemaining;
			double theirTotalStrength = otherDefender.StrengthVersus(opponent, CombatRole.Defense, null) * otherDefender.hitPointsRemaining;
			return ourTotalStrength > theirTotalStrength;
		}


		public void RollToPromote(MapUnit opponent) {
			// Barbarians can't promote.
			if (owner.isBarbarians) {
				return;
			}

			double promotionChance = experienceLevel.promotionChance;
			if (opponent.owner.isBarbarians)
				promotionChance /= 2.0;
			if (owner.civilization.traits.Contains(Civilization.Trait.Militaristic))
				promotionChance *= 2;
			if (GameData.rng.NextDouble() < promotionChance) {
				Promote();
				animate(MapUnit.AnimatedAction.VICTORY);
			}
		}

		public void Promote() {
			ExperienceLevel nextLevel = EngineStorage.gameData.GetExperienceLevelAfter(experienceLevel);
			if (nextLevel != null) {
				experienceLevelKey = nextLevel.key;
				experienceLevel = nextLevel;
				hitPointsRemaining++;
			}
		}

		public double RetreatChance(MapUnit opponent, bool isAttacking) {
			return ((unitType.movement > 1) && (opponent.unitType.movement <= 1)) ? experienceLevel.retreatChance : 0.0;
		}

		internal TileDirection GetAttackAnimationDirection(TileDirection attackDirection) {
			return unitType.rotateBeforeAttack ? attackDirection.rotatedCounterClockwise90Degrees() : attackDirection;
		}

		internal TileDirection GetDefenseAnimationDirection(TileDirection attackDirection) {
			return GetAttackAnimationDirection(attackDirection.reversed());
		}

		public async Task<CombatResult> fight(MapUnit defender) {
			var attacker = this;

			// Set combat animation facing. We'll restore the defender's original facing direction at the end of the battle.
			TileDirection attackerAttackDirection = attacker.location.directionTo(defender.location);
			TileDirection defenderDefenseDirection = attackerAttackDirection.reversed();
			var defenderOriginalDirection = defender.facingDirection;
			attacker.facingDirection = attacker.GetAttackAnimationDirection(attackerAttackDirection);
			defender.facingDirection = defender.GetAttackAnimationDirection(defenderDefenseDirection);

			IEnumerable<StrengthBonus> attackBonuses  = attacker.ListStrengthBonusesVersus(defender, CombatRole.Attack , attackerAttackDirection),
								   defenseBonuses = defender.ListStrengthBonusesVersus(attacker, CombatRole.Defense, attackerAttackDirection);

			double attackerStrength = attacker.unitType.attack  * StrengthBonus.ListToMultiplier(attackBonuses),
			   defenderStrength = defender.unitType.defense * StrengthBonus.ListToMultiplier(defenseBonuses);

			log.Information($"Combat log: {attacker} ({attackerStrength}) attacking {defender} ({defenderStrength})");
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
				double strength = candidate.StrengthVersus(attacker, CombatRole.DefensiveBombard, defenderDefenseDirection);
				if (strength > defensiveBombarderStrength) {
					defensiveBombarder = candidate;
					defensiveBombarderStrength = strength;
				}
			}
			// In the original game, defensive bombard does not trigger against attackers with 1 HP. See:
			// https://github.com/C7-Game/Prototype/pull/250#discussion_r893051111
			if (defensiveBombarder != MapUnit.NONE && attacker.hitPointsRemaining > 1) {
				var dBOriginalDirection = defensiveBombarder.facingDirection;
				TileDirection defensiveBombardDirection = defenderDefenseDirection;
				defensiveBombarder.facingDirection = defensiveBombarder.GetAttackAnimationDirection(defensiveBombardDirection);

				await defensiveBombarder.animateAsync(MapUnit.AnimatedAction.ATTACK1);

				// dADB = defense Against Defensive Bombard
				double dADB = attacker.StrengthVersus(defensiveBombarder, CombatRole.DefensiveBombardDefense, defensiveBombardDirection);
				if (GameData.rng.NextDouble() < defensiveBombarderStrength / (defensiveBombarderStrength + dADB))
					attacker.hitPointsRemaining -= 1;

				defensiveBombarder.defensiveBombardsRemaining -= 1;
				defensiveBombarder.facingDirection = dBOriginalDirection;
			}

			bool defenderEligibleToRetreat = defender.hitPointsRemaining > 1 && ! defender.location.HasCity;

			// Do combat rounds
			while (true) {
				defender.animate(MapUnit.AnimatedAction.ATTACK1);
				await attacker.animateAsync(MapUnit.AnimatedAction.ATTACK1);
				if (GameData.rng.NextDouble() < attackerOdds) {
					if (defenderEligibleToRetreat &&
						defender.hitPointsRemaining == 1 &&
						GameData.rng.NextDouble() < defender.RetreatChance(attacker, false)) {
						// TODO: Defender retreat behavior requires some more work. There's an issue for it here:
						// https://github.com/C7-Game/Prototype/issues/274
						Tile retreatDestination = defender.location.neighbors[attackerAttackDirection];
						if ((retreatDestination != Tile.NONE) && defender.CanEnterTile(retreatDestination, TileProbe.MoveNonAggroProbe())) {
							await defender.move(attackerAttackDirection, true);
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
				alive.RollToPromote(dead);
				await dead.animateAsync(MapUnit.AnimatedAction.DEATH);
				dead.RemoveFromPlay();
			}

			if (result.DefenderWon())
				defender.facingDirection = defenderOriginalDirection;

			return result;
		}

		public bool canBombard() {
			return unitType.actions.Contains(UnitAction.Bombard);
		}

		public bool canBombardTile(Tile tile) {
			if (unitType.bombard == 0)
				return false;

			if (tile.HasImprovements)
				return true;

			MapUnit target = tile.FindTopDefender(this);
			if (target.owner == owner)
				return false;

			if (target != MapUnit.NONE)
				return true;

			if (tile.HasCity && tile.cityAtTile.owner != owner)
				return true;

			return false;
		}


		public async Task bombard(Tile tile) {
			// Could check canBombardTile(..) again, but no need really

			MapUnit target = tile.FindTopDefender(this);

			var hasTargetUnit = target != MapUnit.NONE && target.owner != owner;
			var hasForeignCity = tile.HasCity && tile.cityAtTile.owner != owner;
			var hasCityWalls = hasForeignCity && tile.cityAtTile.GetBuildings().Any(b => b.building.providesWalls);
			var hasTileImprovements = tile.HasImprovements;

			if (!(hasTargetUnit || hasTileImprovements || hasForeignCity))
				return; // Nothing to bombard

			var unitOriginalOrientation = facingDirection;
			facingDirection = location.directionTo(tile);

			if (hasCityWalls)
				await bombardCityWalls(tile);
			else if (hasTargetUnit)
				await bombardUnits(tile, target);
			else if (hasForeignCity)
				await bombardCity(tile);
			else
				await bombardTileImprovements(tile);

			facingDirection = unitOriginalOrientation;
		}

		private async Task bombardUnits(Tile tile, MapUnit target) {
			// TODO: Make configurable

			var hitCount = 0;

			foreach (var fire in Enumerable.Range(0, unitType.rateOfFire)) {
				// TODO: Figure out the bombard defense that walls grant.
				double bombardStrength  = StrengthVersus(target, CombatRole.Bombard, facingDirection);
				double defenderStrength = target.StrengthVersus(this, CombatRole.BombardDefense, facingDirection);
				double attackerOdds = bombardStrength / (bombardStrength + defenderStrength);
				if (Double.IsNaN(attackerOdds))
					return;

				// TODO: Lethal/non-lethal bombardment
				var isPotentiallyLethal = target.hitPointsRemaining == 1;

				await animateAsync(MapUnit.AnimatedAction.ATTACK1);
				movementPoints.onUnitMove(1);
				if (GameData.rng.NextDouble() < attackerOdds && !isPotentiallyLethal) {
					hitCount += 1;
					target.hitPointsRemaining -= 1;
					await tile.AnimateAsync(AnimatedEffect.Hit3);
				} else
					await tile.AnimateAsync(AnimatedEffect.Miss);

				if (target.hitPointsRemaining <= 0) {
					RollToPromote(target);
					await target.animateAsync(MapUnit.AnimatedAction.DEATH);
					target.RemoveFromPlay();
					break; // Target destroyed, skip remaining fire -- TODO: Re-target?
				}
			}

			if (owner.isHuman) {
				if (hitCount > 0)
					new MsgShowTemporaryPopup($"Artillery bombardment successful! Enemy units injured.", tile).send();
				else
					new MsgShowTemporaryPopup($"Artillery bombardment failed.", tile).send();
			}
		}

		private async Task bombardCityWalls(Tile tile) {
			// CF Civilopedia: City walls have a land bombardment defense of 8
			// CF Civilopedia: Coastal defences have a land bombardment defense of 8
			// Anecdotal: "City walls are hit first."
			// TODO: Make configurable

			const int wallDefence = 8;

			var hitCount = 0;

			var walls = tile.cityAtTile.GetBuildings().First(b => b.building.providesWalls);

			foreach (var fire in Enumerable.Range(0, unitType.rateOfFire)) {
				double bombardStrength  = StrengthVersus(null, CombatRole.Bombard, facingDirection);
				double defenderStrength = wallDefence;
				double attackerOdds = bombardStrength / (bombardStrength + defenderStrength);
				if (Double.IsNaN(attackerOdds))
					return;

				await RunAnimatedBombard(tile, attackerOdds, () => {
					hitCount += 1;
					tile.cityAtTile.RemoveBuilding(walls);
				});
			}

			if (owner.isHuman) {
				if (hitCount > 0)
					new MsgShowTemporaryPopup($"Artillery bombardment successful! Walls destroyed.", tile).send();
				else
					new MsgShowTemporaryPopup($"Artillery bombardment failed.", tile).send();
			}
		}

		private async Task RunAnimatedBombard(Tile tile, double attackerOdds, Action callback) {
			await animateAsync(MapUnit.AnimatedAction.ATTACK1);
			movementPoints.onUnitMove(1);
			if (GameData.rng.NextDouble() < attackerOdds) {
				await tile.AnimateAsync(AnimatedEffect.Hit3);
				callback();
			} else
				await tile.AnimateAsync(AnimatedEffect.Miss);
		}

		private async Task bombardCity(Tile tile) {
			// Anecdotal: If there are no units left to hit, then citizens or buildings are hit, apparently with same probability.
			// Anecdotal: "buildings (if I remember correctly) have a defense value of 16"
			// Anecdotal: It seems population is killed off more quickly than buildings.
			// TODO: Make configurable

			const int buildingDefence = 16;
			const int populationDefence = 12;
			const float buildingOrPopulationOdds = 0.5f;

			var targetBuildings = GameData.rng.NextDouble() <= buildingOrPopulationOdds;
			var defence = targetBuildings ? buildingDefence : populationDefence;
			var destroyMsg = targetBuildings ? "Destroyed city population." : "Destroyed a building.";
			Action remover = targetBuildings
				? () =>
				{
					var building = tile.cityAtTile.GetBuildings()
						.OrderBy(x => GameData.rng.Next()).FirstOrDefault();
					tile.cityAtTile.RemoveBuilding(building);
				}
			: () =>
				{
					tile.cityAtTile.RemoveRandomCitizen();
				};

			var hitCount = 0;

			foreach (var fire in Enumerable.Range(0, unitType.rateOfFire)) {
				double bombardStrength  = StrengthVersus(null, CombatRole.Bombard, facingDirection);
				double defenderStrength = defence;
				double attackerOdds = bombardStrength / (bombardStrength + defenderStrength);
				if (Double.IsNaN(attackerOdds))
					return;

				await RunAnimatedBombard(tile, attackerOdds, () => {
					hitCount += 1;
					remover();
				});
			}

			if (owner.isHuman) {
				if (hitCount > 0)
					new MsgShowTemporaryPopup($"Artillery bombardment successful! {destroyMsg}", tile).send();
				else
					new MsgShowTemporaryPopup($"Artillery bombardment failed.", tile).send();
			}
		}

		private async Task bombardTileImprovements(Tile tile) {
			// Anecdotal: "arty seems to wipe out improvement on 75% or more of the shots"
			// ==> Artillery.bombard : 12 --> TileImprovement.Defense : 3
			// TODO: Make configurable

			const int tileImprovementDefence = 3;

			var hitCount = 0;

			var improvement = tile.overlays.GetImprovements()
				.OrderBy(x => GameData.rng.Next()).FirstOrDefault();

			foreach (var fire in Enumerable.Range(0, unitType.rateOfFire)) {
				double bombardStrength  = StrengthVersus(null, CombatRole.Bombard, facingDirection);
				double defenderStrength = tileImprovementDefence;
				double attackerOdds = bombardStrength / (bombardStrength + defenderStrength);
				if (Double.IsNaN(attackerOdds))
					return;

				await RunAnimatedBombard(tile, attackerOdds, () => {
					hitCount += 1;
					tile.overlays.Remove(improvement);
					// TODO: Re-target?
				});
			}

			if (owner.isHuman) {
				if (hitCount > 0)
					new MsgShowTemporaryPopup($"Artillery bombardment successful! Destroyed {improvement?.key}.", tile).send();
				else
					new MsgShowTemporaryPopup($"Artillery bombardment failed.", tile).send();
			}
		}

		public int HealRateAt(Tile location) {
			GameData gD = EngineStorage.gameData;
			City city = location.cityAtTile;
			bool inFriendlyCity = (city != null) && (city != City.NONE) && owner.IsAtPeaceWith(city.owner);
			if (inFriendlyCity)
				return gD.healRateInCity;
			if (unitType.categories.Contains("Sea"))
				return 0;
			return gD.healRateInNeutralField;
			// TODO: Consider friendly/neutral/enemy territory once that's implemented, barracks, the Red Cross
		}

		public void OnBeginTurn(bool skipTurn = false) {
			int maxMP = unitType.movement;
			if (movementPoints.remaining >= maxMP && !skipTurn) {
				int maxHP = maxHitPoints;
				if (hitPointsRemaining < maxHP)
					hitPointsRemaining += HealRateAt(location);
				if (hitPointsRemaining > maxHP)
					hitPointsRemaining = maxHP;
			}

			if (skipTurn) {
				movementPoints.skipTurn();
			} else {
				movementPoints.reset(maxMP);
			}

			defensiveBombardsRemaining = 1;
		}

		public void OnEnterTile(Tile tile) {
			//Add to player knowledge of tiles
			owner.tileKnowledge.AddTilesToKnown(tile);

			// Disperse barb camp
			if (tile.hasBarbarianCamp && !owner.isBarbarians) {
				EngineStorage.gameData.map.barbarianCamps.Remove(tile);
				tile.hasBarbarianCamp = false;
				animate(MapUnit.AnimatedAction.VICTORY);

				// TODO: make this configurable
				owner.gold += 25;
				if (owner.isHuman) {
					new MsgShowMilitaryAdvisorPopup($"We cleared a barbarian encampment and earned 25 gold!", happy: true).send();
				}
			}

			// Destroy the enemy city on the tile unless we're the barbarians,
			// in which case we'll just take some gold.
			if (tile.HasCity && !owner.IsAtPeaceWith(tile.cityAtTile.owner)) {
				if (owner.isBarbarians) {
					// TODO: Add rules for how much gold is taken.
					int goldTaken = tile.cityAtTile.owner.gold / 4;
					tile.cityAtTile.owner.gold -= goldTaken;
					this.RemoveFromPlay();
					if (tile.cityAtTile.owner.isHuman) {
						new MsgShowMilitaryAdvisorPopup($"Barbarians have stolen {goldTaken} gold from our cities!\nWe need a stronger military.", happy: false).send();
					}
				} else {
					CityInteractions.DestroyCity(tile.XCoordinate, tile.YCoordinate);
				}
			}

			// Check to see if we've discovered a new civ.
			//
			// TODO: this should really be based on interactions with our "visible"
			// tiles. Also civ3 only counts border-based discovery from rank 1
			// tiles, not rank 2+.
			foreach (Tile t in tile.neighbors.Values) {
				if (t.unitsOnTile.Count > 0 && owner != t.unitsOnTile[0].owner) {
					owner.EnsureRelationshipExists(t.unitsOnTile[0].owner);
				}
				if (t.owningCity != null && owner != t.owningCity.owner) {
					owner.EnsureRelationshipExists(t.owningCity.owner);
				}
			}
		}

		// Generalized check to see whether a given tile is accessible to the unit in a given context.
		public bool CanEnterTile(Tile tile, TileProbe probe) {
			if (this.owner.isHuman && !this.owner.HasExploredTile(tile))
				return true;

			// TODO: Add sea/ocean restrictions for water units
			// Check if the player has the appropriate tech or wonder to move freely.
			// Civ3 seems to not allow to set a destination on sea/ocean without tech/wonder
			// (even if you can actually go there using the keyboard keys)
			// but it will go through it if it's just a shortcut to an allowed tile.

			// Keep land units on land and sea units on water
			if (this.IsWaterUnit() && tile.IsLand()) {
				if (tile.HasCity && tile.cityAtTile.owner == owner) {
					return true;
				}
				return false;
			}
			// TODO: to be modified to allow boarding on ships,
			// that have the capacity and can take units of this kind
			if (this.IsLandUnit() && !tile.IsLand())
				return false;

			if (!this.HasRank()) {
				if (HasHostileCity(tile, owner)) {
					if (probe.RaiseNotice) {
						new MsgShowTemporaryPopup($"Only combat units can capture cities and improvements.", location).send();
					}
					return false;
				}
				if (HasHostileUnits(tile, owner) || (tile.hasBarbarianCamp && !this.owner.isBarbarians)) {
					if (probe.RaiseNotice) {
						new MsgShowTemporaryPopup($"Non-combat units may not attack.", location).send();
					}
					return false;
				}
			}

			// If we allow declaring war on this move, then it doesn't matter if
			// there are units belonging to another player on the tile.
			// TODO: unbreakable alliances
			if (probe.AllowWarDeclaration) {
				return true;
			}

			// Allow AI to "see" human/other AI units even if they are in an unexplored or inactive tile
			// from their perspective, but allow humans to set a course if the tile
			// is unknown or not active, aka, the human player shouldn't know what's on the tile
			// if the tile is not active, or they haven't  discovered it yet.
			//
			// If we don't want to have the AI have this advantage over the human player
			// we can simply remove the !isHuman() condition. This can also be tied to
			// AI difficulty level, or be made moddable somehow, etc...
			if (!this.owner.isHuman || this.owner.tileKnowledge.isActiveTile(tile)) {
				// Check for units belonging to other civs
				foreach (MapUnit other in tile.unitsOnTile) {
					if (other.owner != owner) {
						if (!other.owner.IsAtPeaceWith(owner))
							return probe.AllowCombat && unitType.attack > 0;
						return false;
					}
				}
			}

			// Check for cities belonging to other civs.
			if (tile.cityAtTile != null && tile.cityAtTile.owner != owner) {
				if (!this.owner.isHuman || this.owner.tileKnowledge.isActiveTile(tile))
					return probe.AllowCombat;
				return false;
			}

			return true;
		}

		private static bool HasHostileUnits(Tile tile, Player player) {
			foreach (MapUnit other in tile.unitsOnTile) {
				if (player != other.owner && !player.IsAtPeaceWith(other.owner))
					return true;
			}
			return false;
		}

		private static bool HasHostileCity(Tile tile, Player player) {
			return tile.HasCity && !player.IsAtPeaceWith(tile.cityAtTile.owner);
		}

		/// <summary>
		/// Moves the unit in the given direction
		/// </summary>
		/// <param name="unit"></param>
		/// <param name="dir">Which direction to move, e.g. northeast, west, etc.</param>
		/// <param name="wait">Whether the method should wait to return until animations complete</param>
		/// <returns>True if the unit is alive after the movement, false otherwise</returns>
		/// <exception cref="Exception"></exception>
		public async Task<bool> move(TileDirection dir, bool wait = false) {
			(int dx, int dy) = dir.toCoordDiff();
			Tile newLoc = EngineStorage.gameData.map.tileAt(dx + location.XCoordinate, dy + location.YCoordinate);
			if ((newLoc != Tile.NONE) && CanEnterTile(newLoc, TileProbe.MoveAggroWithNoticeProbe()) && (movementPoints.canMove)) {
				facingDirection = dir;
				wake();

				// Trigger combat if the tile we're moving into has an enemy  Or if this unit can't fight, do nothing.
				MapUnit defender = newLoc.FindTopDefender(this);
				if (defender != MapUnit.NONE && !owner.IsAtPeaceWith(defender.owner)) {
					if (unitType.attack > 0) {
						CombatResult combatResult = await fight(defender);
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
							if (newLoc.FindTopDefender(this) != MapUnit.NONE) {
								movementPoints.onUnitMove(1);
								return true;
							}

							// Similarly if we retreated, pay one MP for the combat but don't move.
						} else if (combatResult == CombatResult.AttackerRetreated) {
							movementPoints.onUnitMove(1);
							return true;
						}
					} else {
						return true;
					}
				}

				facingDirection = dir;
				float movementCost = TilePath.GetMovementCost(this.owner, location, dir, newLoc);
				if (!location.unitsOnTile.Remove(this))
					throw new System.Exception("Failed to remove unit from tile it's supposed to be on");
				// Make sure the unit is on the new location before claiming we have entered the tile
				newLoc.unitsOnTile.Add(this);
				location = newLoc;
				OnEnterTile(newLoc);

				if (wait)
					await animateAsync(MapUnit.AnimatedAction.RUN);
				else
					animate(MapUnit.AnimatedAction.RUN);

				movementPoints.onUnitMove(movementCost);
			}
			return true;
		}

		private float SumWorkerProgress(Tile tile, Terraform workerJob) {
			float result = 0;
			foreach (MapUnit unit in tile.unitsOnTile) {
				if (unit.WorkerJob == workerJob) {
					result += unit.WorkerProgressTowardsJob;
				}
			}
			return result;
		}

		public int TurnsToCompleteTerraform(Terraform t) {
			// Figure out how much work remains to do on this particular job.
			int remainingTerraformCost = GetWorkerJobCost(location, t) - (int)SumWorkerProgress(location, t);

			// Figure out how fast all of the wokers doing this particular
			// terraform will work.
			float combinedWorkerSpeed = workerSpeed();
			foreach (MapUnit unit in location.unitsOnTile) {
				if (unit.WorkerJob == t) {
					combinedWorkerSpeed += unit.workerSpeed();
				}
			}

			// Divide the two, rounding up.
			return (int)Math.Ceiling(remainingTerraformCost / combinedWorkerSpeed);
		}

		public async Task PerformBusyAction() {
			if (isFortified) {
				return;
			}

			if (path != null && path.PathLength() > 0) {
				await moveAlongPath();
				return;
			}

			// Check to see if we have a worker job, and if so, contribute our
			// work towards it. We do this before any automation logic, so that
			// automated units properly contribute their efforts.
			if (WorkerJob != null) {
				WorkerProgressTowardsJob += workerSpeed();
				movementPoints.onConsumeAll();

				// See if this worker finished the job.
				if ((int)SumWorkerProgress(location, WorkerJob) >= GetWorkerJobCost(location, WorkerJob)) {
					location.FinishWorkerJob(WorkerJob);
				}
			}

			if (isAutomated) {
				playAutomatedTurn();
				return;
			}
		}

		public async Task moveAlongPath() {
			while (movementPoints.canMove && path?.PathLength() > 0) {
				TileDirection dir = location.directionTo(path.Next());
				await move(dir, true); //TODO: don't wait on last move animation?
			}
		}

		public async Task setUnitPath(TilePath path) {
			this.path = path;
			await moveAlongPath();
		}

		public void skipTurn() {
			movementPoints.skipTurn();
		}

		public async Task Disband() {
			await EngineStorage.gameData.DisbandUnit(this);
		}

		public void RemoveFromPlay() {
			EngineStorage.gameData.RemoveUnit(this);
		}

		public bool canBuildCity() {
			if (!unitType.actions.Contains(UnitAction.BuildCity)) {
				return false;
			}
			if (location.HasCity || !location.IsAllowCities()) {
				return false;
			}
			return location.neighbors.Values.All(tile => !tile.HasCity);
		}

		public async Task<City?> buildCity(string cityName) {
			if (!canBuildCity()) {
				log.Warning($"can't build city at {location}");
				return null;
			}

			await animateAsync(MapUnit.AnimatedAction.BUILD);

			// TODO: Need to check somewhere that this unit is allowed to build a city on its current tile. Either do that here or in every caller
			// (probably best to just do it here).
			City city = CityInteractions.BuildCity(location, owner, cityName);
			this.RemoveFromPlay();

			return city;
		}

		public bool canPerformTerraformAction(Terraform terraform) {
			return unitType.terraformActions.Contains(terraform) && terraform.MeetsRequirements(owner, location);
		}

		public bool canPerformTerraformAction(Terraform terraform, Tile tile) {
			return unitType.terraformActions.Contains(terraform) && terraform.MeetsRequirements(owner, tile);
		}

		public void PerformTerraformAction(Terraform terraform) {
			if (!canPerformTerraformAction(terraform)) {
				log.Warning($"can't perform {terraform.Name} by {this}");
				return;
			}
			WorkerJob = terraform;

			if (terraform.Animation is AnimatedAction animation)
				animate(animation, AnimationEnding.Repeat);

			wake();
			_ = PerformBusyAction();
		}

		public float workerSpeed() {
			float progressPerTurn = this.IsCaptive() ? JOB_PROGRESS_SLAVE : JOB_PROGRESS_WORKER;
			if (owner.civilization.traits.Contains(Civilization.Trait.Industrious)) {
				progressPerTurn *= 1.5f;
			}
			return progressPerTurn;
		}

		public void resetWorkerJob() {
			WorkerJob = null;
			WorkerProgressTowardsJob = 0;
			animate(MapUnit.AnimatedAction.BLANK, AnimationEnding.Repeat);
		}

		public bool canAutomate() {
			return unitType.actions.Contains(UnitAction.Automate);
		}

		public void automate() {
			wake();
			isAutomated = true;
			WorkerAIData? maybeAiData = WorkerAI.MakeAiData(this, owner);
			if (maybeAiData == null) {
				log.Information($"Could not find anything to automate for {this} owned by {owner}");
				isAutomated = false;
				return;
			}
			currentAI = new WorkerAI(maybeAiData);
			playAutomatedTurn();
		}

		public bool canExplore() {
			return unitType.actions.Contains(UnitAction.Explore);
		}

		public void explore() {
			wake();
			isAutomated = true;
			ExplorerAIData? maybeAiData = ExplorerAI.MaybeMakeAiData(this, owner);
			if (maybeAiData == null) {
				log.Information($"Could not find anything to explore for {this} owned by {owner}");
				isAutomated = false;
				return;
			}
			currentAI = new ExplorerAI(maybeAiData);
			playAutomatedTurn();
		}

		public async void playAutomatedTurn() {
			if (currentAI == null) {
				// TODO: handle giving automated workers from loaded saves the
				// proper unit ai.
				isAutomated = false;
				return;
			}
			UnitAI.Result result = await currentAI.PlayTurn(owner, this);
			if (result == UnitAI.Result.Done) {
				if (currentAI is WorkerAI) {
					automate();
				} else if (currentAI is ExplorerAI) {
					explore();
				}
			}

			// Do nothing after an error so control returns to the player, and
			// nothing after an progress result, so that next turn continues the
			// AI action.
		}

		public List<Terraform> GetAvailableTerraforms() {
			return EngineStorage.gameData.Terraforms.Where(canPerformTerraformAction).ToList();
		}

		/**
		 * Helper function to get the available actions for a unit
		 * based on what terrain it is on.
		 **/
		public List<UnitAction> GetAvailableActions() {
			List<UnitAction> result = new();

			// Eventually, we should look this up somewhere to see what all actions we have (and mods might add more)
			// For now, this is still an improvement over the last iteration.
			UnitAction[] implementedActions = { UnitAction.Hold, UnitAction.Wait, UnitAction.Fortify, UnitAction.Disband, UnitAction.Goto, UnitAction.Bombard };
			foreach (UnitAction action in implementedActions) {
				if (unitType.actions.Contains(action)) {
					result.Add(action);
				}
			}

			if (canBuildCity()) {
				result.Add(UnitAction.BuildCity);
			}
			if (canExplore()) {
				result.Add(UnitAction.Explore);
			}
			if (canAutomate()) {
				result.Add(UnitAction.Automate);
			}

			// Eventually we will have advanced actions too, whose availability will rely on their base actions' availability.
			// unit.availableActions.Add("rename");

			return result;
		}
	}

	public struct TileProbe {
		public bool AllowCombat { get; init; }
		public bool AllowWarDeclaration { get; init; }
		public bool RaiseNotice { get; init; }

		public static TileProbe MoveNonAggroProbe() {
			return new TileProbe();
		}

		public static TileProbe MoveAggroProbe() {
			return new TileProbe() { AllowCombat = true };
		}

		public static TileProbe MoveAggroWithNoticeProbe() {
			return new TileProbe() { AllowCombat = true, RaiseNotice = true };
		}

		public static TileProbe DeclareWarProbe() {
			return new TileProbe() { AllowCombat = true, AllowWarDeclaration = true };
		}
	}
}
