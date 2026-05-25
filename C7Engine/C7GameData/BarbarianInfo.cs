using System.Text.Json.Serialization;

namespace C7GameData {
	/// <summary>
	/// Contains info pertaining to barbarian setup.
	/// This was in the catch-all RULE in Civ3.  I'm giving it its own class in part
	/// because we may want to add more customization options.
	/// </summary>
	public class BarbarianInfo {
		//Legacy Civ3-compatible config
		public string basicBarbarianUnit;
		public string advancedBarbarianUnit;
		public string barbarianSeaUnit;
		[JsonIgnore] public UnitPrototype basicBarbarian;
		[JsonIgnore] public UnitPrototype advancedBarbarian;
		[JsonIgnore] public UnitPrototype barbarianSeaUnitProto;

		public int defaultHitpoints = 2;
		public int maxHitpoints = 2;

		public BarbarianActivity barbarianActivity = BarbarianActivity.Roaming;
	}
}
