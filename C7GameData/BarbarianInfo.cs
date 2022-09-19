using System.Text.Json.Serialization;

namespace C7GameData {
	/// <summary>
	/// Contains info pertaining to barbarian setup.
	/// This was in the catch-all RULE in Civ3.  I'm giving it its own class in part
	/// because we may want to add more customization options.
	/// </summary>
	public class BarbarianInfo {
		//Legacy Civ3-compatible config
		public int basicBarbarianIndex = 0;
		public int advancedBarbarianIndex = 0;
		public int barbarianSeaUnitIndex = 0;
		[JsonIgnore] public UnitPrototype basicBarbarian;
		[JsonIgnore] public UnitPrototype advancedBarbarian;
		[JsonIgnore] public UnitPrototype barbarianSeaUnit;

		public int defaultHitpoints = 2;
		public int maxHitpoints = 2;
	}
}
