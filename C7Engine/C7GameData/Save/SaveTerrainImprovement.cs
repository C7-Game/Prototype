using System.Collections.Generic;
using Layer = C7GameData.TerrainImprovement.Layer;

namespace C7GameData.Save;

public class SaveTerrainImprovement {
	// The following method is used to generate terrain improvement
	// data when loading a CIV3 SAV or BIQ file
	public static IEnumerable<SaveTerrainImprovement> Civ3Improvements() {
		yield return new("irrigation", Layer.ResourceDevelopment, zIndex: 0);
		yield return new("mine", Layer.ResourceDevelopment, zIndex: 2);

		yield return new("road", Layer.Roads, movementCost: 1.0f / 3, zIndex: 1);
		yield return new(
			"railroad",
			Layer.Roads,
			upgradesFrom: "road",
			movementCost: 0,
			zIndex: 1,
			tileModifier: "terrain_improvements.railroad.tile_modifier"
		);

		yield return new(
			"fortress",
			Layer.Holdings,
			defenseBonus: new("fortress", 0.5),
			zIndex: 3
		);
		yield return new(
			"barricade",
			Layer.Holdings,
			upgradesFrom: "fortress",
			defenseBonus: new("barricade", 1),
			zIndex: 3
		);

		// TODO: Add colony, outpost, airfield, radar tower
	}

	public readonly string key;

	public readonly Layer layer;
	public readonly StrengthBonus? defenseBonus;

	public readonly int zIndex;

	public readonly float movementCost = -1;

	public readonly string upgradesFrom; // a key for another Terrain Improvement

	// The string in the key points to a Terrain Type
	public Dictionary<string, Dictionary<Tile.YieldType, int>> bonusYields = [];

	// Path to Lua function
	public readonly string tileModifier;

	public SaveTerrainImprovement(
		string key,
		Layer layer,
		string upgradesFrom = null,
		StrengthBonus? defenseBonus = null,
		float movementCost = -1,
		int zIndex = 0,
		string tileModifier = null
	) {
		this.key = key;
		this.layer = layer;
		this.defenseBonus = defenseBonus;
		this.upgradesFrom = upgradesFrom;
		this.movementCost = movementCost;
		this.zIndex = zIndex;
		this.tileModifier = tileModifier;
	}
}
