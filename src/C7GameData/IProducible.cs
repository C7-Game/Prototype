namespace C7GameData {
	/**
	 * Represents something that can be produced by a city.
	 * Known examples are Buildings and UnitPrototypes.
	 */
	public interface IProducible {
		string name { get; set; }
		int shieldCost { get; set; }
		int populationCost { get; set; }
	}
}
