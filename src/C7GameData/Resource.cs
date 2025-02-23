namespace C7GameData {
	public class Resource {
		public string Key { get; set; }
		public int Index { get; set; }  //used so we can wire up tiles to resources later
		public int Icon { get; set; }
		public int FoodBonus { get; set; }
		public int ShieldsBonus { get; set; }
		public int CommerceBonus { get; set; }
		public ResourceCategory Category { get; set; }
		public int AppearanceRatio { get; set; }
		public int DisappearanceRatio { get; set; }
		public string Name { get; set; }
		public string CivilopediaEntry { get; set; }
		//public Technology Prerequisite { get; set; }	//Uncomment when we have technologies

		public static readonly Resource NONE = new Resource
		{
			Key = "NONE",
			Index = -1,
			Category = ResourceCategory.NONE
		};

		public override string ToString() {
			return $"Resource {Index} named {Name} of type {Category} with bonuses {FoodBonus}, {ShieldsBonus}, {CommerceBonus}";
		}
	}

	public enum ResourceCategory {
		BONUS,
		LUXURY,
		STRATEGIC,
		NONE
	}
}
