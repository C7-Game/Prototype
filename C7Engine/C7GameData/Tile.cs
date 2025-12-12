namespace C7GameData {
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using C7GameData.Save;
	using C7Engine;
	using System.Threading.Tasks;

	public class Tile {
		public enum YieldType {
			Commerce,
			Food,
			Production
		}

		public class Yield {
			public static Yield CalculateForCity(Tile tile, int yield, YieldType type, City city) {
				return new Yield(tile, yield, type)
						.ApplyTerrainImprovementModifiers(tile)
						.ApplyCityModifiers(city)
						.ApplyPlayerModifiers(city.owner);
			}

			public static Yield CalculateForPlayer(Tile tile, int yield, YieldType type, Player player) {
				return new Yield(tile, yield, type)
						.ApplyTerrainImprovementModifiers(tile)
						.ApplyPlayerModifiers(player);
			}

			public Yield(Tile tile, int baseYield, YieldType type) {
				this.tile = tile;
				this.baseYield = baseYield;
				this.type = type;
			}

			private Yield ApplyPlayerModifiers(Player player) {
				player.government.tileModifier?.Invoke(this);
				return this;
			}

			private Yield ApplyCityModifiers(City city) {
				city.GetBuildings().ForEach(b => b.building.tileModifier?.Invoke(this));
				return this;
			}

			private Yield ApplyTerrainImprovementModifiers(Tile tile) {
				tile.overlays.GetImprovements().ToList().ForEach(ti => ti.tileModifier?.Invoke(this));
				return this;
			}

			public readonly Tile tile;
			public readonly YieldType type;
			public int penalty = 0;
			public int bonus = 0;
			public readonly int baseYield = 0;
			public int yield { get => baseYield + bonus - penalty; }
		}

		public ID Id { get; internal set; }
		public Civ3ExtraInfo ExtraInfo;
		public int XCoordinate;
		public int YCoordinate;

		// Needed for coordinate wrapping.
		public GameMap map;

		// An arbitrary number indicating which landmass this tile is part of,
		// for land-based tiles, or -1 for water.
		//
		// This is used to avoid the expensive process of pathfinding between
		// two land tiles just to discover they have no land connection.
		public int continent;

		// For water tiles, is this tile part of an inland sea with fresh water?
		public bool isFreshWater = false;

		// An arbitrary number indicating which part of the continent this tile
		// is part of, for the purposes of biome assignment.
		public int biomeRegion = -1;

		public City owningCity; // The city whose border contains this tile
		public TerrainType baseTerrainType = TerrainType.NONE;
		public TerrainType overlayTerrainType = TerrainType.NONE;

		private City _cityAtTile;
		public City cityAtTile {
			get { return _cityAtTile; }
			set {
				_cityAtTile = value;
				if (value != null) {
					ClearTerrainOverlay();
					overlays.Clear();
				}
			}
		}

		public bool HasCity => cityAtTile != null && cityAtTile != City.NONE;
		public CityResident personWorkingTile = null;   //allows us to see if another city is working this tile
		public bool hasBarbarianCamp = false;
		//One thing to decide is do we want to have a tile have a list of units on it,
		//or a unit have reference to the tile it is on, or both?
		//The downside of both is that both have to be updated (and it uses a miniscule amount
		//of memory for pointers), but I'm inclined to go with both since it makes it easy and
		//efficient to perform calculations, whether you need to know which unit on a tile
		//has the best defense, or which tile a unit is on when viewing the Military Advisor.
		public List<MapUnit> unitsOnTile = new List<MapUnit>();
		public string ResourceKey { get; set; }
		public Resource Resource { get; set; }

		public Dictionary<TileDirection, Tile> neighbors { get; set; } = new Dictionary<TileDirection, Tile>();

		//See discussion on page 4 of the "Babylon" thread (https://forums.civfanatics.com/threads/0-1-babylon-progress-thread.673959) about sub-terrain type and Civ3 properties.
		//We may well move these properties somewhere, whether that's Civ3ExtraInfo, a Civ3Tile child class, a Dictionary property, or something else, in the future.
		public bool isBonusShield;
		public bool isSnowCapped;
		public bool isPineForest;

		public bool riverNorth;
		public bool riverNortheast;
		public bool riverEast;
		public bool riverSoutheast;
		public bool riverSouth;
		public bool riverSouthwest;
		public bool riverWest;
		public bool riverNorthwest;

		// The first time a forest is cleared on a tile it can award shields to
		// a nearby city.
		public bool hasHadForestCleared = false;

		public TileOverlays overlays;

		public Tile(ID id) {
			this.Id = id;
			unitsOnTile = new List<MapUnit>();
			Resource = Resource.NONE;
			overlays = new(this);
		}

		// TODO: this should be either an extension in C7Engine, or otherwise
		// calculated somewhere else, but it's not obvious to someone unfamiliar
		// with the save format that it's the overaly terrain that has actual
		// movement cost
		public int MovementCost() {
			return overlayTerrainType.movementCost;
		}

		public static Tile NONE = new Tile(ID.None("tile")) {
			XCoordinate = -1,
			YCoordinate = -1,
		};

		//This should be used when we want to check if land tiles are next to water tiles.
		//Usually this is coast, but it could be Sea - see the "Deepwater Harbours" topics at CFC.
		//Sometimes we care *specifically* about the Coast terrain, e.g. galleys can only move on that terrain, not Sea or Ocean
		//Those cases should not use this method.
		public bool NeighborsWater() {
			foreach (Tile neighbor in neighbors.Values) {
				if (neighbor.baseTerrainType.isWater()) {
					return true;
				}
			}
			return false;
		}

		public bool NeighborsFreshWater() {
			foreach (Tile neighbor in neighbors.Values) {
				if (neighbor.baseTerrainType.isWater() && neighbor.isFreshWater) {
					return true;
				}
			}
			return false;
		}

		public bool NeighborsOcean() {
			foreach (Tile neighbor in neighbors.Values) {
				if (neighbor.baseTerrainType.isWater() && !neighbor.isFreshWater) {
					return true;
				}
			}
			return false;
		}

		/// <summary>
		/// Returns neighbors along edges only.
		/// This is used by some graphics algorithms.
		/// </summary>
		/// <returns></returns>
		public Tile[] getEdgeNeighbors() {
			Tile[] edgeNeighbors =  { neighbors[TileDirection.NORTHEAST], neighbors[TileDirection.NORTHWEST], neighbors[TileDirection.SOUTHEAST], neighbors[TileDirection.SOUTHWEST]};
			return edgeNeighbors;
		}

		public override string ToString() {
			return "[" + XCoordinate + ", " + YCoordinate + "] (" + overlayTerrainType.Key + " on " + baseTerrainType.Key + ")";
		}

		public List<Tile> GetLandNeighbors() {
			return neighbors.Values.Where(tile => tile != NONE && !tile.baseTerrainType.isWater()).ToList();
		}

		/**
		 * Returns neighbors of the "Coast" type, not including Sea or Ocean.  This is used e.g. for Galley movement.
		 * Eventually, this should be refactored into a more general "get valid neighbors to move to" type of method,
		 * which could work e.g. for units that can move anywhere except desert.
		 **/
		public List<Tile> GetCoastNeighbors() {
			return neighbors.Values.Where(tile => tile.baseTerrainType.Key == "coast").ToList();
		}

		public bool HasRiverCrossing(TileDirection dir) {
			switch (dir) {
				case TileDirection.NORTH: return riverNorth;
				case TileDirection.NORTHEAST: return riverNortheast;
				case TileDirection.EAST: return riverEast;
				case TileDirection.SOUTHEAST: return riverSoutheast;
				case TileDirection.SOUTH: return riverSouth;
				case TileDirection.SOUTHWEST: return riverSouthwest;
				case TileDirection.WEST: return riverWest;
				case TileDirection.NORTHWEST: return riverNorthwest;
				default: throw new ArgumentOutOfRangeException("Invalid TileDirection");
			}
		}

		public bool IsLand() {
			return !baseTerrainType.isWater();
		}

		public bool IsWater() {
			return baseTerrainType.isWater();
		}

		public bool IsAllowCities() {
			return overlayTerrainType.allowCities && !hasBarbarianCamp;
		}

		public bool IsVolcano() {
			return overlayTerrainType.isVolcano();
		}

		public TileDirection directionTo(Tile other) {
			if ((this == NONE) || (other == NONE))
				throw new System.Exception("Can't get direction toward NONE Tile since it doesn't have a meaningful location");

			// We have to use the map helper functions to handle edge wrapping
			// correctly.
			//
			// y calculation is reversed so dy is in typical Cartesian coords
			// instead of tile coords, where y is inverted
			int dx = map.CalculateXDelta(other.XCoordinate, this.XCoordinate);
			int dy = map.CalculateYDelta(this.YCoordinate, other.YCoordinate);
			double angle = Math.Atan2(dy, dx); // angle is in interval [-pi, pi]

			if (angle > 7.0 / 8.0 * Math.PI) return TileDirection.WEST;
			else if (angle > 5.0 / 8.0 * Math.PI) return TileDirection.NORTHWEST;
			else if (angle > 3.0 / 8.0 * Math.PI) return TileDirection.NORTH;
			else if (angle > 1.0 / 8.0 * Math.PI) return TileDirection.NORTHEAST;
			else if (angle > -1.0 / 8.0 * Math.PI) return TileDirection.EAST;
			else if (angle > -3.0 / 8.0 * Math.PI) return TileDirection.SOUTHEAST;
			else if (angle > -5.0 / 8.0 * Math.PI) return TileDirection.SOUTH;
			else if (angle > -7.0 / 8.0 * Math.PI) return TileDirection.SOUTHWEST;
			else return TileDirection.WEST;
		}

		/**
		 * Distance as the raven flies to another tile.
		 * This is a rough metric only.
		 */
		public int distanceTo(Tile other) {
			if (this == Tile.NONE || other == Tile.NONE) {
				// We can't path to tiles that don't exist.
				return int.MaxValue;
			}
			return (Math.Abs(map.CalculateXDelta(other.XCoordinate, this.XCoordinate)) + Math.Abs(map.CalculateYDelta(other.YCoordinate, this.YCoordinate))) / 2;
		}

		// Returns the number of "ranks" to another tile, where each rank is a
		// border expansion due to culture. So rank 1 is immediate neighbors,
		// rank 2 is the "big fat cross", etc.
		public int rankDistanceTo(Tile other) {
			// Get the x and y deltas in the standard grid coordinates.
			int dx = Math.Abs(map.CalculateXDelta(other.XCoordinate, this.XCoordinate));
			int dy = Math.Abs(map.CalculateYDelta(other.YCoordinate, this.YCoordinate));

			// Transform that to the rank distance using the formula from
			// https://forums.civfanatics.com/threads/everything-about-corruption-c3c-edition.76619/post-1551201
			return (dx + dy) / 2 + Math.Abs(dx - dy) / 4;
		}

		private int baseFoodYield(Player player) {
			int yield = overlayTerrainType.baseFoodProduction;
			if (this.Resource != Resource.NONE && player.KnowsAboutResource(Resource)) {
				yield += this.Resource.FoodBonus;
			}

			if (HasCity) {
				// All city centers have a food yield of 2, regardless of bonus
				// food. See https://wiki.civforum.de/wiki/Stadtfeldertrag_(Civ3).
				yield = 2;

				// TODO: For agricultural civilizations, the city field produces
				// a food yield of three food, but this is reduced to two by the
				// despotism penalty, unless the city is located on a fresh
				// water source or has already reached city size (≥ 7)
			}

			yield += overlays.GetBaseYieldBonus(YieldType.Food);

			return yield;
		}

		public Yield foodYield(Player player) {
			int yield = baseFoodYield(player);
			return Yield.CalculateForPlayer(this, yield, YieldType.Food, player);
		}

		public Yield foodYield(City city) {
			int yield = baseFoodYield(city.owner);
			return Yield.CalculateForCity(this, yield, YieldType.Food, city);
		}

		private int baseProductionYield(Player player) {
			int yield = overlayTerrainType.baseShieldProduction;
			if (overlayTerrainType.Key == "grassland" && this.isBonusShield) {
				yield++;
			}

			if (HasCity) {
				// City centers always have 1 shield prior to any bonuses
				// resources, regardless of the terrain.
				// See https://wiki.civforum.de/wiki/Stadtfeldertrag_(Civ3).
				yield = 1;

				// There is a size bonus for larger cities.
				if (cityAtTile.residents.Count >= 7 && cityAtTile.residents.Count < 13) {
					yield += 1;
				} else if (cityAtTile.residents.Count >= 13) {
					yield += 2;

					// TODO: +1 more for industrial civs.
				}
			}

			// Bonus resources provide a boost in yield regardless of whether
			// there is a city.
			if (Resource != Resource.NONE && player.KnowsAboutResource(Resource)) {
				yield += this.Resource.ShieldsBonus;
			}

			yield += overlays.GetBaseYieldBonus(YieldType.Production);

			return yield;
		}

		public Yield productionYield(Player player) {
			int yield = baseProductionYield(player);
			return Yield.CalculateForPlayer(this, yield, YieldType.Production, player);
		}

		public Yield productionYield(City city) {
			int yield = baseProductionYield(city.owner);
			return Yield.CalculateForCity(this, yield, YieldType.Production, city);
		}

		private int baseCommerceYield(Player player) {
			int yield = overlayTerrainType.baseCommerceProduction;
			if (this.Resource != Resource.NONE && player.KnowsAboutResource(Resource)) {
				yield += this.Resource.CommerceBonus;
			}
			if (BordersRiver()) {
				yield += 1;
			}

			// See https://wiki.civforum.de/wiki/Stadtfeldertrag_(Civ3)
			if (HasCity) {
				int regularCityYield;
				if (cityAtTile.residents.Count < 7) {
					regularCityYield = 1;
				} else if (cityAtTile.residents.Count < 13) {
					regularCityYield = 2;
				} else {
					regularCityYield = 3;
				}
				if (BordersRiver()) {
					regularCityYield += 1;
				}
				if (this.Resource != Resource.NONE && player.KnowsAboutResource(Resource)) {
					regularCityYield += this.Resource.CommerceBonus;
				}

				int capitalCityYield = 0;
				if (cityAtTile.IsCapital()) {
					capitalCityYield = 4;
				}

				yield = Math.Max(regularCityYield, capitalCityYield);
			}

			yield += overlays.GetBaseYieldBonus(YieldType.Commerce);

			return yield;
		}

		public Yield commerceYield(Player player) {
			int yield = baseCommerceYield(player);

			// TODO: handle the commerce bonus for costal cities+seafaring
			// TODO: handle the commerce bonus for commerial civs
			return Yield.CalculateForPlayer(this, yield, YieldType.Commerce, player);
		}

		public Yield commerceYield(City city) {
			int yield = baseCommerceYield(city.owner);

			return Yield.CalculateForCity(this, yield, YieldType.Commerce, city);
		}

		public bool BordersRiver() {
			return riverNorth || riverNortheast || riverEast || riverSoutheast || riverSouth || riverSouthwest || riverWest || riverNorthwest;
		}

		// TODO: This method doesn't handle the electicity tech which allows
		// irrigating without fresh water access.
		public bool CanBeIrrigated(TerrainImprovement irrigation, Player player) {
			// Irrigation can't be done if there is no irrigation bonus for the
			// tile or if there's already an improvement or city on the tile.
			if (!overlays.CanAdd(irrigation) ||
				irrigation.GetYieldBonus(overlayTerrainType, YieldType.Food) <= 0 ||
				cityAtTile != null) {
				return false;
			}

			// If a tile borders a river or fresh water, it has fresh water access.
			if (BordersRiver() || NeighborsFreshWater()) {
				return true;
			}

			foreach (KeyValuePair<TileDirection, Tile> dirToTile in neighbors) {
				// If a neighboring tile is irrigated, this tile has fresh water access.
				if (dirToTile.Value.overlays.HasImprovement(irrigation)) {
					return true;
				}

				// Special case, if we are neighboring a city, check
				// if the city can act as part of an irrigation chain.
				if (dirToTile.Value.cityAtTile != null) {
					if (dirToTile.Value.BordersRiver() || dirToTile.Value.NeighborsFreshWater()) {
						return true;
					}

					foreach (var (dir, tile) in dirToTile.Value.neighbors) {
						if (tile.overlays.HasImprovement(irrigation)) {
							return true;
						}
					}
				}
			}

			return false;
		}

		//Convenience method for printing the yield
		public string YieldString(Player player) {
			return $"{foodYield(player).yield}/{productionYield(player).yield}/{commerceYield(player).yield})";
		}

		public Player? OwningPlayer() {
			if (cityAtTile != null) {
				return cityAtTile.owner;
			}
			if (owningCity != null) {
				return owningCity.owner;
			}
			return null;
		}

		public void MaybeAwardForestClearingShields() {
			if (hasHadForestCleared) {
				return;
			}
			hasHadForestCleared = true;

			// Shields can only be awarded if the forest is within some city's
			// borders.
			if (OwningPlayer() == null) {
				return;
			}

			// Check all the tiles of the forest that a city could be in, taking into account the big fat cross size.
			foreach (Tile other in GetTilesWithinRankDistance(EngineStorage.gameData.rules.MaxRankOfWorkableTiles)) {
				if (other.cityAtTile == null) {
					continue;
				}

				// Shields aren't awarded to wonders.
				if (other.cityAtTile.itemBeingProduced is Building b
					&& (b.greatWonderProperties != null || b.isSmallWonder)) {
					continue;
				}

				City c = other.cityAtTile;
				int shieldsAwarded = EngineStorage.gameData.rules.ForestValueInShields;
				c.shieldsStored += shieldsAwarded;
				c.shieldsStored = Math.Min(c.shieldsStored, c.owner.ShieldCost(c.itemBeingProduced));
				new MsgShowTemporaryPopup($"{shieldsAwarded} shields awarded for clearing forests", other).send();
				return;
			}
		}

		// Returns the X and Y coordinates of the neighbor in the specified direction.
		public static TileLocation NeighborCoordinate(TileLocation location, TileDirection direction) {
			switch (direction) {
				case TileDirection.NORTH:
					location.Y -= 2;
					break;
				case TileDirection.NORTHEAST:
					location.Y--;
					location.X++;
					break;
				case TileDirection.EAST:
					location.X += 2;
					break;
				case TileDirection.SOUTHEAST:
					location.Y++;
					location.X++;
					break;
				case TileDirection.SOUTH:
					location.Y += 2;
					break;
				case TileDirection.SOUTHWEST:
					location.Y++;
					location.X--;
					break;
				case TileDirection.WEST:
					location.X -= 2;
					break;
				case TileDirection.NORTHWEST:
					location.X--;
					location.Y--;
					break;
			}
			return location;
		}

		// Returns the tile at a "neighbor index", where 0 is this tile, 1 is
		// due north, 2 is NE, 3 is E, and so on in a clockwise spiral.
		// Index 9 is N+N, 10 is N+NE, etc.
		//
		// This is slightly different than the civ3 spiral, which starts with
		// the NE and goes clockwise. Ring 2 of the civ3 spiral is the BFC tiles,
		// but rings beyond that get stranger. We don't need to match the civ3
		// spiral exactly, and this is much simpler to understand.
		public Tile GetTileAtNeighborIndex(int neighborIndex) {
			// Special case: Index 0 is this tile.
			if (neighborIndex <= 0) {
				return this;
			}

			int xDelta = 0;
			int yDelta = 0;

			// Figure out which ring we're in.
			int ringNumber = 0;
			do {
				ringNumber++;
			} while (Math.Pow(2 * ringNumber + 1, 2) <= neighborIndex);

			// Figure out how many tiles are in the previous ring.
			// For ring 2, we get (2*2 - 1)^2, which is 9.
			int cellsInInnerRings = (ringNumber * 2 - 1) * (ringNumber * 2 - 1);

			// Figure out the index of this neighbor within our ring.
			int indexInRing1Based = neighborIndex - cellsInInnerRings;

			// Our ring is a square with 4 sides, and each side has
			// (ringNumber*2 + 1) tiles in it. But then we have the overlap of
			// each corner, so excluding the overlap we have ringNumber*2 tiles
			// per side.
			//
			// For ring 1, the 4 sections of size 2 are
			//    (N, NE), (E, SE), (S, SW), (W, NW)
			int cellsPerSquareEdge = ringNumber * 2;

			// Define segment boundaries based on 1-based index within the ring
			int segment1End = cellsPerSquareEdge;
			int segment2End = 2 * cellsPerSquareEdge;
			int segment3End = 3 * cellsPerSquareEdge;
			int segment4End = 4 * cellsPerSquareEdge;

			if (indexInRing1Based <= segment1End) {
				// This is the side that goes from N to 1 short of E.
				// N and NE for ring 1.
				xDelta = indexInRing1Based;
				yDelta = indexInRing1Based - cellsPerSquareEdge;
			} else if (indexInRing1Based <= segment2End) {
				// This is the side that goes from E to 1 short of S.
				// E and SE for ring 1.
				xDelta = segment2End - indexInRing1Based;
				yDelta = indexInRing1Based - cellsPerSquareEdge;
			} else if (indexInRing1Based <= segment3End) {
				// This is the side that goes from S to 1 short of W.
				// S and SW for ring 1.
				xDelta = segment2End - indexInRing1Based;
				yDelta = segment3End - indexInRing1Based;
			} else {
				// This is the side that goes from W to 1 short of N.
				// W and NW for ring 1.
				xDelta = indexInRing1Based - segment4End;
				yDelta = segment3End - indexInRing1Based;
			}

			return map.tileAt(XCoordinate + xDelta, YCoordinate + yDelta);
		}

		// Returns the tiles in the spiral ordering defined by
		// GetTileAtNeighborIndex(i).
		public List<Tile> GetTilesWithinRankDistance(int rank) {
			List<Tile> result = new();
			for (int i = 0; i < (rank * 2 + 1) * (rank * 2 + 1); ++i) {
				Tile t = GetTileAtNeighborIndex(i);
				if (rankDistanceTo(t) <= rank) {
					result.Add(t);
				}
			}

			return result;
		}

		public MapUnit FindTopDefender(MapUnit opponent) {
			if (unitsOnTile.Count > 0) {
				IEnumerable<MapUnit> potentialDefenders = unitsOnTile.Where(u => u.CanDefendAgainst(opponent));
				if (potentialDefenders.Count() == 0) {
					return MapUnit.NONE;
				}

				MapUnit leadingCandidate = unitsOnTile[0];
				foreach (MapUnit u in unitsOnTile)
					if (u.HasPriorityAsDefender(leadingCandidate, opponent))
						leadingCandidate = u;
				return leadingCandidate;
			} else
				return MapUnit.NONE;
		}

		/// <summary>
		/// Disbands non-defending units on a tile.  This should only be called when all defending units have been destroyed,
		/// hence its name.  E.g. if only air/sea units remain after a land battle, this should be called.
		///
		/// Eventually, we should also have a method to make relevant units (workers, artillery, etc.) be captured.
		/// </summary>
		/// <param name="tile"></param>
		public void DisbandNonDefendingUnits(Player owner) {
			//There may have been naval units, if so, disband them
			if (unitsOnTile.Count > 0) {
				//Copy to a separate array so we don't crash due to concurrent modification exceptions
				MapUnit[] unitsOnTile = new MapUnit[this.unitsOnTile.Count];
				this.unitsOnTile.CopyTo(unitsOnTile);
				foreach (MapUnit destroyedUnit in unitsOnTile) {
					// Ensure we only destroy units of the losing side of the
					// combat, not the unit entering the city.
					if (destroyedUnit.owner == owner) {
						destroyedUnit.disband();
					}
				}
			}
		}

		/// <summary>
		/// After a WorkerJob has finished, Cclean up all the WorkerJobs and set the correct overlay
		/// </summary>
		/// <param name="tile">the current tile</param>
		/// <param name="currentWorkerJob">the worker job currently finished, must not be null</param>
		public void FinishWorkerJob(Terraform currentWorkerJob) {
			// Reset All Workers working on the finished Job
			Player player = null;
			foreach (MapUnit unit in unitsOnTile) {
				player = unit.owner;
				if (currentWorkerJob == unit.WorkerJob) {
					unit.resetWorkerJob();
				}
			}

			currentWorkerJob.OnComplete(player, this);
		}

		public async Task AnimateAsync(AnimatedEffect effect) {
			if (!EngineStorage.animationsEnabled) return;

			var msg = new MsgStartEffectAnimation(this, effect, AnimationEnding.Stop);
			msg.send();

			await EngineStorage.WaitForAnimationFinished(msg.animationId);
		}

		public void Animate(AnimatedEffect effect) {
			_ = AnimateAsync(effect);
		}

		public void ClearTerrainOverlay() {
			overlayTerrainType = baseTerrainType;
		}
	}

	public enum TileDirection {
		NORTH,
		NORTHEAST,
		EAST,
		SOUTHEAST,
		SOUTH,
		SOUTHWEST,
		WEST,
		NORTHWEST,
	}

	public static class TileDirectionExtensions {
		public static TileDirection reversed(this TileDirection dir) {
			switch (dir) {
				case TileDirection.NORTH: return TileDirection.SOUTH;
				case TileDirection.NORTHEAST: return TileDirection.SOUTHWEST;
				case TileDirection.EAST: return TileDirection.WEST;
				case TileDirection.SOUTHEAST: return TileDirection.NORTHWEST;
				case TileDirection.SOUTH: return TileDirection.NORTH;
				case TileDirection.SOUTHWEST: return TileDirection.NORTHEAST;
				case TileDirection.WEST: return TileDirection.EAST;
				case TileDirection.NORTHWEST: return TileDirection.SOUTHEAST;
				default: throw new ArgumentOutOfRangeException("Invalid TileDirection");
			}
		}

		public static (int, int) toCoordDiff(this TileDirection dir) {
			switch (dir) {
				case TileDirection.NORTH: return (0, -2);
				case TileDirection.NORTHEAST: return (1, -1);
				case TileDirection.EAST: return (2, 0);
				case TileDirection.SOUTHEAST: return (1, 1);
				case TileDirection.SOUTH: return (0, 2);
				case TileDirection.SOUTHWEST: return (-1, 1);
				case TileDirection.WEST: return (-2, 0);
				case TileDirection.NORTHWEST: return (-1, -1);
				default: throw new ArgumentOutOfRangeException("Invalid TileDirection");
			}
		}
	}

	public class TileOverlays {
		private readonly Tile tile;
		private Dictionary<TerrainImprovement.Layer, TerrainImprovement> terrainImprovementByLayer = [];

		public TileOverlays(Tile tile) {
			this.tile = tile;
		}

		public void Add(TerrainImprovement improvement) {
			if (!CanAdd(improvement))
				throw new InvalidOperationException($"Cannot add {improvement.key} to the tile");

			terrainImprovementByLayer.TryGetValue(improvement.layer, out TerrainImprovement replacedImprovement);

			terrainImprovementByLayer[improvement.layer] = improvement;

			// If a road is being added to a tile that previously had
			// no road, invalidate the cached trade network
			if (improvement.layer == TerrainImprovement.Layer.Roads && replacedImprovement == null) {
				// Hack: don't do this if gamedata is null, which can
				// be true in some unit tests.
				EngineStorage.gameData?.InvalidateCachedTradeNetwork();
			}
		}

		public TerrainImprovement ImprovementAtLayer(TerrainImprovement.Layer layer) {
			terrainImprovementByLayer.TryGetValue(layer, out TerrainImprovement ti);
			return ti;
		}

		public TerrainImprovement ImprovementAtLayer(Terraform terraform) {
			return terraform.Improvement == null ? null : ImprovementAtLayer(terraform.Improvement.layer);
		}

		public bool HasImprovement(TerrainImprovement improvement) {
			return terrainImprovementByLayer.TryGetValue(improvement.layer, out TerrainImprovement val) && val == improvement;
		}

		public IEnumerable<TerrainImprovement> GetImprovements() {
			return terrainImprovementByLayer.Values;
		}

		public bool CanAdd(TerrainImprovement improvement) {
			if (tile.HasCity)
				return false;

			if (!terrainImprovementByLayer.TryGetValue(improvement.layer, out var current))
				return improvement.upgradesFrom == null;

			return current.CanBeReplacedBy(improvement);
		}

		public bool HasRoad() {
			return terrainImprovementByLayer.ContainsKey(TerrainImprovement.Layer.Roads) || tile.HasCity;
		}

		// Will return a -1 if the tile movement cost is unaffected by the improvements
		public float MovementCost() {
			if (terrainImprovementByLayer.TryGetValue(TerrainImprovement.Layer.Roads, out TerrainImprovement road)) {
				return road.movementCost;
			}

			if (tile.HasCity) {
				return 0;
			}

			return -1;
		}

		public int GetBaseYieldBonus(Tile.YieldType type) {
			return terrainImprovementByLayer.Values.Sum(ti => ti.GetYieldBonus(tile.overlayTerrainType, type));
		}

		public bool HasBeenImproved() {
			return terrainImprovementByLayer.Count > 0;
		}

		public IEnumerable<StrengthBonus> GetDefenseBonuses() {
			return terrainImprovementByLayer.Values
				.Select(ti => ti.defenseBonus)
				.Where(v => v.HasValue)
				.Select(v => v.Value);
		}

		public void Clear() {
			terrainImprovementByLayer.Clear();
		}
	}
}
