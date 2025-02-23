using System;
using System.Collections.Generic;
using System.Linq;
using Serilog;

namespace C7GameData {
	public struct CommerceBreakdown {
		public int taxes;
		public int beakers;
		public int happiness;
	}

	public class City {
		public ID id { get; set; }
		public Tile location { get; internal set; }
		public string name;
		public int size = 1;
		public Dictionary<Player, int> perPlayerCulture = new();

		//Temporary production code because production is fun.
		public IProducible itemBeingProduced;
		public int shieldsStored = 0;

		public int foodStored = 0;
		public int foodNeededToGrow = 20;

		public bool capital = false;
		public Player owner { get; set; }
		public List<CityResident> residents = new List<CityResident>();

		public static City NONE = new City(Tile.NONE, null, "Dummy City", ID.None("city"));

		public City(Tile location, Player owner, string name, ID id) {
			this.id = id;
			this.location = location;
			this.owner = owner;
			this.name = name;
			if (owner != null) {
				this.perPlayerCulture.Add(owner, 0);
			}
		}

		internal City() { }

		public void SetItemBeingProduced(IProducible producible) {
			this.itemBeingProduced = producible;
		}

		public bool IsCapital() {
			return capital;
		}

		public bool CanBuildUnit(UnitPrototype proto) {
			List<string> allowedUnits = new List<string> {"Warrior", "Chariot", "Settler", "Worker", "Catapult", "Galley"};
			if (!allowedUnits.Contains(proto.name))
				return false;
			if (proto.categories.Contains("Sea"))
				return location.NeighborsWater();
			else
				return true;
		}

		public int TurnsUntilGrowth() {
			if (FoodGrowthPerTurn() == 0) {
				return int.MaxValue;
			}
			int additionalFoodNeeded = foodNeededToGrow - foodStored;
			int turnsRoundedDown = additionalFoodNeeded / FoodGrowthPerTurn();
			if (additionalFoodNeeded % FoodGrowthPerTurn() != 0) {
				return turnsRoundedDown + 1;
			}
			return turnsRoundedDown;
		}

		public int TurnsToProduce(IProducible item) {
			int additionalProductionNeeded = item.shieldCost - shieldsStored;
			int turnsRoundedDown = additionalProductionNeeded / CurrentProductionYield();
			if (additionalProductionNeeded % CurrentProductionYield() != 0) {
				return turnsRoundedDown + 1;
			}
			return turnsRoundedDown;
		}

		public int TurnsUntilProductionFinished() {
			return TurnsToProduce(itemBeingProduced);
		}

		public void ComputeCityGrowth() {
			foodStored += FoodGrowthPerTurn();
			if (foodStored >= foodNeededToGrow) {
				size++;
				foodStored = 0;
			} else if (foodStored < 0) {
				size--;
				foodStored = 0;
			}
		}

		/**
		 * Computes turn production.  If the production queue finishes,
		 * returns the item that is built.  Otherwise, returns null.
		 */
		public IProducible ComputeTurnProduction() {

			shieldsStored += CurrentProductionYield();
			if (shieldsStored >= itemBeingProduced.shieldCost && size > itemBeingProduced.populationCost) {
				shieldsStored = 0;
				size -= itemBeingProduced.populationCost;
				return itemBeingProduced;
			}

			shieldsStored = Math.Min(shieldsStored, itemBeingProduced.shieldCost);
			return null;
		}

		public int CurrentFoodYield() {
			int yield = location.foodYield(owner);
			foreach (CityResident r in residents) {
				yield += r.tileWorked.foodYield(owner);
			}
			return yield;
		}

		public int CurrentProductionYield() {
			int yield = location.productionYield(owner);
			foreach (CityResident r in residents) {
				yield += r.tileWorked.productionYield(owner);
			}
			return yield;
		}

		public CommerceBreakdown CurrentCommerceYield() {
			int yield = location.commerceYield(owner);
			foreach (CityResident r in residents) {
				yield += r.tileWorked.commerceYield(owner);
			}

			CommerceBreakdown result = new CommerceBreakdown();
			result.beakers = (int)Math.Floor(yield * owner.scienceRate / 10.0);
			result.happiness = (int)Math.Floor(yield * owner.luxuryRate / 10.0);
			result.taxes = yield - result.beakers - result.happiness;

			return result;
		}

		public int FoodGrowthPerTurn() {
			return CurrentFoodYield() - size * 2;
		}

		private void RemoveCitizen() {
			residents[residents.Count - 1].tileWorked.personWorkingTile = null;
			residents.RemoveAt(residents.Count - 1);
		}

		public void RemoveCitizens(int number) {
			for (int i = 0; i < number; i++) {
				if (residents.Count > 0) {
					RemoveCitizen();
				} else {
					Log.Warning("Trying to remove last citizen from " + name);
					break;
				}
			}
		}

		public void RemoveAllCitizens() {
			while (residents.Count > 0) {
				RemoveCitizen();
			}
		}

		public override string ToString() {
			return $"{name} ({size})";
		}

		public int GetCulture() {
			return perPlayerCulture[owner];
		}

		public int GetBorderExpansionLevel() {
			// Give ourselves a minimum of 1 culture to avoid taking the log of 0
			int culture = Math.Max(1, GetCulture());

			// Take the log10 of culture, rounding down (so a culture of 123
			// would be 2, a culture of 5 would be 0, etc) and then add one to
			// get the expansion level. With 0-9 culture our culture goal is 10^1
			// and we have one tile of borders, with 10-99 our culture goal is
			// 10^2 and we have two tiles of borders.
			return (int)Math.Floor(Math.Log10(culture)) + 1;
		}

		// The list of tiles that could be worked by this city.
		// This isn't necessarily a subset of our borders, because we're allowed
		// to work tiles owned by our civ in our big fat cross, even if our
		// borders haven't expanded yet.
		//
		// TODO: we should make this configurable to allow for the bigger cross
		// if a mod wants it.
		public HashSet<Tile> GetWorkableTiles() {
			HashSet<Tile> result = new();
			foreach (Tile t in GetTilesOfRank(2)) {
				// Skip tiles not owned by this player.
				if (t.owningCity == null || t.owningCity.owner != this.owner) {
					continue;
				}

				// Skip tiles with cities on them.
				if (t.HasCity) {
					continue;
				}

				result.Add(t);
			}
			return result;
		}

		// The list of tiles that are within the borders of this city, without
		// taking into account border collisions with other cities.
		public HashSet<Tile> GetTilesWithinBorders() {
			return GetTilesOfRank(GetBorderExpansionLevel());
		}

		private HashSet<Tile> GetTilesOfRank(int rank) {
			HashSet<Tile> result = new();
			HashSet<Tile> knownTiles = new();

			Stack<Tile> toCheck = new();
			toCheck.Push(location);
			knownTiles.Add(location);

			while (toCheck.Count > 0) {
				Tile t = toCheck.Pop();

				// Skip tiles that are too far away.
				if (location.rankDistanceTo(t) > rank) {
					continue;
				}

				// Ocean tiles may only hold claims of rank 2.
				if (t.baseTerrainTypeKey == "ocean" && rank > 2) {
					continue;
				}

				// Otherwise this tile is close enough. Check its neighbors next.
				result.Add(t);
				foreach (Tile neighbor in t.neighbors.Values) {
					if (!knownTiles.Contains(neighbor)) {
						toCheck.Push(neighbor);
						knownTiles.Add(t);
					}
				}
			}

			return result;
		}
	}
}
