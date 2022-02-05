namespace C7GameData
{
	/**
	 * Represents something that can be produced by a city.
	 * Known examples are Buildings and UnitPrototypes.
	 */
	public abstract class IProducable
	{
		public string name { get; set; }
		public int shieldCost { get; set; }
		public int populationCost { get; set; }
	}
}
