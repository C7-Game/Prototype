using System.Collections.Generic;

namespace C7GameData {
	/**
	 * Represents something that can be produced by a city.
	 * Known examples are Buildings and UnitPrototypes.
	 */
	public interface IProducible {
		string name { get; set; }
		int populationCost { get; set; }
		Tech requiredTech { get; set; }
		HashSet<Resource> requiredResources { get; set; }

		int ShieldCost(HashSet<Civilization.Trait> civTraits, float costFactor);

		bool CanProduce(City city, HashSet<Resource> accessibleResources);
	}
}
