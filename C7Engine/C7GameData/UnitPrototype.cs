using System.Collections.Generic;

namespace C7GameData {
	using System;
	using System.Linq;
	using C7Engine;
	using Save;

	public enum UnitAction {
		BuildCity,
		Bombard,
		Hold,
		Wait,
		Fortify,
		Disband,
		Goto,
		Explore,
		Automate
	}

	public struct ItemContext(UnitPrototype proto, Player player) {
		public UnitPrototype proto = proto;
		public Player player = player;
	}

	// A container for all the art for this unit
	public struct Art {
		public MainArt mainArt;
		public ThumbNailArt thumbnailArt;
		public PediaArt pediaArt;
	}

	// the main art contains the Animation art folder path for the unit
	public struct MainArt {
		public string defaultName;
		public Dictionary<string, string> variations;
	}
	// the thumbnail art contains the index to the small unit icons used in city screen production, etc
	public struct ThumbNailArt {
		public int defaultIndex;
		public Dictionary<string, int> variations;
	}
	// the icons being used by the civilopedia and also other places like the Science Advisor
	public struct PediaArt {
		public string small;
		public string large;
	}

	/**
	 * The prototype for a unit, which defines the characteristics of a unit.
	 * For example, a Spearman might have 1 attack, 2 defense, and 1 movement.
	 **/
	public class UnitPrototype : IProducible {
		public string name { get; set; }
		public Art art { get; set; }
		public int shieldCost { get; set; }
		public int populationCost { get; set; }
		public Tech requiredTech { get; set; }
		public int attack { get; set; }
		public int defense { get; set; }
		public int bombard { get; set; }
		public int bombardRange { get; set; }
		public int rateOfFire { get; set; }
		public int movement { get; set; }
		public HashSet<Civilization> producibleBy { get; set; } = [];
		public UnitPrototype upgradeTo;
		public bool unproducible;
		public HashSet<SaveUnitPrototype.Flag> flags = [];
		public bool rotateBeforeAttack {
			get => flags.Contains(SaveUnitPrototype.Flag.RotateBeforeAttack);
			set {
				if (value) {
					flags.Add(SaveUnitPrototype.Flag.RotateBeforeAttack);
				} else {
					flags.Remove(SaveUnitPrototype.Flag.RotateBeforeAttack);
				}
			}
		}

		public HashSet<string> categories = new HashSet<string>();

		public HashSet<UnitAction> actions = [];

		public HashSet<string> attributes = new HashSet<string>();

		public HashSet<Resource> requiredResources { get; set; } = [];

		public HashSet<Terraform> terraformActions = [];

		public bool isWorker => terraformActions.Count > 0;

		public UnitPrototype() { }

		public UnitPrototype(SaveUnitPrototype proto, IEnumerable<Terraform> terraforms) {
			(name, art, shieldCost, populationCost, attack, defense, bombard, bombardRange, rateOfFire, movement, unproducible) =
			(proto.name, proto.art, proto.shieldCost, proto.populationCost,
			 proto.attack, proto.defense, proto.bombard, proto.bombardRange, proto.rateOfFire, proto.movement, proto.unproducible);

			categories = new HashSet<string>(proto.categories);
			actions = proto.actions;
			attributes = new HashSet<string>(proto.attributes);
			flags = new HashSet<SaveUnitPrototype.Flag>(proto.flags);

			terraformActions = proto.terraformActions.Select(id => terraforms.First(t => t.Id == id)).ToHashSet();
		}

		public int ShieldCost(HashSet<Civilization.Trait> civTraits, float costFactor) {
			return (int)(shieldCost * costFactor);
		}

		public bool IsLandUnit() {
			return categories.Contains("Land");
		}

		public bool IsSeaUnit() {
			return categories.Contains("Sea");
		}

		public override string ToString() {
			return $"{name} ({attack}/{defense}/{movement})";
		}

		public double BaseStrength(CombatRole role) {
			switch (role) {
				case CombatRole.Attack: return attack;
				case CombatRole.Defense: return defense;
				case CombatRole.Bombard: return bombard;
				case CombatRole.BombardDefense: return defense;
				case CombatRole.DefensiveBombard: return bombard;
				case CombatRole.DefensiveBombardDefense: return defense;
				default: throw new ArgumentOutOfRangeException("Invalid CombatRole");
			}
		}

		public MapUnit GetInstance(ID id, UnitPrototype proto, Player owner, Civilization nationality = null, Tile location = null, TileDirection facingDirection = TileDirection.SOUTHWEST, int hitPoints = 3) {
			MapUnit instance = new MapUnit(id);
			instance.unitType = proto;
			instance.name = proto.name;
			instance.hitPointsRemaining = hitPoints;    //todo: make this configurable
			instance.owner = owner;
			if (nationality != null)
				instance.nationality = nationality;
			else
				instance.nationality = owner.civilization;
			instance.location = location;

			instance.movementPoints.reset(movement);
			return instance;
		}

		private UnitPrototype GetUnitUpgrade(Civilization civ) {
			while (true) {
				if (upgradeTo == null) return null;
				if (!upgradeTo.producibleBy.Contains(civ)) {
					upgradeTo.GetUnitUpgrade(civ);
				}

				return upgradeTo;
			}
		}

		public bool CanProduce(City city, HashSet<Resource> accessibleResources) {
			var unitUpgradeChain = city.owner.civilization.GetUpgradeChain(this);
			return city.owner.civilization.IsUnitAvailable(this)
				   && this.MeetsProductionRequirements(city, accessibleResources)
				   && !this.IsUnitObsolete(city, accessibleResources);
		}

		// TODO: Consider golden ages when determining whether a unit is obsolete.
		// If a golden age has not yet been triggered and a unit can trigger one,
		// it shouldn't be marked as obsolete, even if its upgrade is available.
		private bool IsUnitObsolete(City city, HashSet<Resource> accessibleResources) {
			if (EngineStorage.gameData.rules.AllowLesserUnitProduction) return false;

			if (this.GetProducibleUpgrade(city, accessibleResources) == null) return false;

			return true;
		}

		// For example, if we want to check if the Trebuchet is obsolete for the Koreans,
		// what we can do, because we don't want to hardcode anywhere that
		// the Hwacha is the "replacement" to the Cannon (which is Trebuchet's upgrade)
		// we can check if the upgrade (Artillery) of the upgrade (Cannon) of our unit (Trebuchet)
		// has other units that upgrade to it and are available to the Koreans.
		// This is how we get that, since we can built a Hwacha,
		// which upgrades to Artillery, that the Trebuchet is obsolete.
		private List<UnitPrototype> GetUnitsThatUpgradeToThisUpgrade() {
			List<UnitPrototype> allUnits = EngineStorage.gameData.unitPrototypes.Where(
				p => p.upgradeTo != null && p.upgradeTo == this.upgradeTo?.upgradeTo
					).ToList();
			return allUnits;
		}

		// This should be the method we use to get the "true" upgrade of a given unit
		public UnitPrototype GetProducibleUpgrade(City city, HashSet<Resource> accessibleResources) {
			UnitPrototype upgrade = this.GetUnitUpgrade(city.owner.civilization);
			if (upgrade == null) return null;

			var unitUpgradeChain = city.owner.civilization.GetUpgradeChain(this);

			var potentialUnits = this.GetUnitsThatUpgradeToThisUpgrade();

			var units = unitUpgradeChain.Concat(potentialUnits.Where(uu => !unitUpgradeChain.Contains(uu)));

			// picking the last item, as when trying to get the upgrade for a Warrior,
			// and Medieval Infantry is available, we don't want to return the Swordsman
			var unitUpgrade = units.LastOrDefault(uu =>
				uu.MeetsProductionRequirements(city, accessibleResources)
				&& uu.producibleBy.Contains(city.owner.civilization)
			);

			return unitUpgrade;
		}

		private bool MeetsProductionRequirements(City city, HashSet<Resource> accessibleResources) {
			if (!city.owner.HasRequiredTechnology(this)) {
				return false;
			}

			if (this.IsSeaUnit() && !city.location.NeighborsWater()) {
				return false;
			}

			if (!this.requiredResources.All(accessibleResources.Contains)) {
				return false;
			}

			return true;
		}
	}
}
