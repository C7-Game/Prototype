using System;
using System.Text.Json.Serialization;
using QueryCiv3.Biq;
using System.Collections.Generic;

namespace C7GameData {

	// TerrainFile represents the data in a json file declaring terrain types
	public class TerrainFile: IJsonOnDeserialized {
		public TerrainMetadata Metadata;
		public Dictionary<string, TerrainType> Types;

		public void OnDeserialized() {
			foreach ((string k, TerrainType t) in this.Types) {
				t.Key = String.Format("{0}/{1}", this.Metadata.Mod, k);
			}
		}
	}

	// TerrainMetadata is the metadata associated with a TerrainFile
	public struct TerrainMetadata {
		public string Mod;
	}

	public class TerrainType {
		// The "key" is a language-independent name for the terrain. Civ3 relies on their index in the list to know
		// what they are, while C7 uses the key. This allows adding custom terrain types in the future, including having
		// different custom terrains in Mod A than Mod B, while still allowing internationalized versions of their
		// names that don't break the scenario. E.g. "ocean". When a terrain named "foo" in a terrain file for mod "bar"
		// is imported, the key is "foo/bar".
		[JsonIgnore]
		public string Key { get; set; } = "";
		//The name is the display name.  E.g. "Ocean" in English scenarios, "Hochsee" in German scenarios.
		public string DisplayName { get; set; } = "";
		public int baseFoodProduction { get; set; }
		public int baseShieldProduction { get; set; }
		public int baseCommerceProduction { get; set; }
		public int movementCost { get; set; }
		public bool allowCities { get; set; } = true;
		public bool hilly;
		public bool water;
		public StrengthBonus defenseBonus;

		//some stuff about graphics would probably make sense, too

		public bool isHilly() => this.hilly;

		public bool isWater() => this.water;

		public override string ToString() {
			return DisplayName + "(" + baseFoodProduction + ", " + baseShieldProduction + ", " + baseCommerceProduction + ")";
		}

		public static TerrainType NONE = new TerrainType();


		// TODO: this should lookup terrains defined in terrain.json
		public static TerrainType ImportFromCiv3(int civ3Index, TERR civ3Terrain) {
			TerrainType c7Terrain = new TerrainType();
			c7Terrain.Key = civTerrainKeyLookup[civ3Index];
			c7Terrain.water = civ3Index >= 11 && civ3Index <= 13; // coast, sea, or ocean
			c7Terrain.hilly = civ3Index == 5 || civ3Index == 6 || civ3Index == 10; // hills, mountains, volcano
			c7Terrain.DisplayName = civ3Terrain.Name;
			c7Terrain.baseFoodProduction = civ3Terrain.Food;
			c7Terrain.baseShieldProduction = civ3Terrain.Shields;
			c7Terrain.baseCommerceProduction = civ3Terrain.Commerce;
			c7Terrain.movementCost = civ3Terrain.MovementCost;
			c7Terrain.allowCities = civ3Terrain.AllowCities != 0;
			c7Terrain.defenseBonus = new StrengthBonus {
				description = civ3Terrain.Name,
				amount = civ3Terrain.DefenseBonus / 100.0
			};
			return c7Terrain;
		}

		//This only works for Conquests due to the new terrains being added in the middle of the list.
		private static Dictionary<int, string> civTerrainKeyLookup = new Dictionary<int, string>() {
			{ 0,  "desert"},
			{ 1,  "plains"},
			{ 2,  "grassland"},
			{ 3,  "tundra"},
			{ 4,  "flood plain"},
			{ 5,  "hills"},
			{ 6,  "mountains"},
			{ 7,  "forest"},
			{ 8,  "jungle"},
			{ 9,  "marsh"},
			{ 10, "volcano"},
			{ 11, "coast"},
			{ 12, "sea"},
			{ 13, "ocean"}
		};
	}
}
