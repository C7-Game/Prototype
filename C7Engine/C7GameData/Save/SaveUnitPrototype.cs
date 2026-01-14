using System.Collections.Generic;
using System.Linq;

namespace C7GameData.Save {
	public class SaveUnitPrototype {
		public class Unique {
			public string replace;
			public string civilization;
		};

		public string name { get; set; }
		public string artName { get; set; }
		public int shieldCost { get; set; }
		public int populationCost { get; set; }
		public ID requiredTech { get; set; }
		public int attack { get; set; }
		public int defense { get; set; }
		public int bombard { get; set; }
		public int movement { get; set; }
		public int iconIndex { get; set; }

		public string upgradeTo;
		public Unique unique;
		public bool unproducible;

		public HashSet<string> categories = new HashSet<string>();

		public HashSet<UnitAction> actions = [];

		public HashSet<string> attributes = new HashSet<string>();

		public HashSet<string> requiredResources = [];

		public HashSet<ID> terraformActions = [];

		public SaveUnitPrototype() { }

		public SaveUnitPrototype(UnitPrototype proto) {
			(name, artName, shieldCost, populationCost, unproducible,
			attack, defense, bombard, movement, iconIndex) =
			(proto.name, proto.artName, proto.shieldCost, proto.populationCost, proto.unproducible,
			 proto.attack, proto.defense, proto.bombard, proto.movement, proto.iconIndex);

			if (proto.requiredTech != null)
				requiredTech = proto.requiredTech.id;

			if (proto.upgradeTo != null)
				upgradeTo = proto.upgradeTo.name;

			if (proto.unique != null) {
				unique = new() {
					civilization = proto.unique.civilization.name,
					replace = proto.unique.replace?.name
				};
			}

			categories = new HashSet<string>(proto.categories);
			actions = proto.actions;
			attributes = new HashSet<string>(proto.attributes);

			requiredResources = proto.requiredResources.Select(r => r.Key).ToHashSet();
			terraformActions = proto.terraformActions.Select(r => r.Id).ToHashSet();
		}
	}
}
