namespace C7GameData {
	public class Building : IProducible {
		public string name { get; set; }
		public int shieldCost { get; set; }
		public int populationCost { get; set; } // Will always be equal to 0 in the Civ3 rule set 
	}
}