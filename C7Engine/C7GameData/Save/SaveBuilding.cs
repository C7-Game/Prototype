using System.Collections.Generic;
using System.Linq;

namespace C7GameData.Save {
	public class SaveBuilding {
		public enum Flag {
			IsCenterOfEmpire,
			VeteranGroundUnits,
			VeteranSeaUnits,
			MustBeCoastal,
			MustBeNearRiver,
			IncreasesLuxuryTrade,
			ReducesCorruption,
			ForbiddenPalace,
			IncreasesShieldsInWater,
			IncreasesFoodInWater,
			IncreasesTradeInWater,
			AllowsCitySize2,
			AllowsCitySize3,
			DoublesCityGrowthRate,
		}
		public string name;
		public int shieldCost;
		public int populationCost;
		public ID requiredTech;
		public string requiredBuilding;
		public bool isGreatWonder;
		public bool isSmallWonder;
		public int culturePerTurn;
		public int contentFacesInCity;
		public int iconRowIndex;
		public ID? renderedObsoleteBy;

		// The name of the building this building gives to every city in the
		// empire on on the continent (like the pyramids or the internet).
		public string buildingGainedInEveryCity;
		public string buildingGainedInEveryCityOnContinent;

		// Assorted boolean flags for the building. They're stored in this set
		// rather than as booleans to avoid bloating the json file.
		public HashSet<Flag> flags = new();

		public HashSet<string> requiredResources = [];

		public SaveBuilding() { }
	}
}
