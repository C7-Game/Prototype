using System;
using System.Collections.Generic;
using C7Engine;

namespace C7GameData.Save;

public class SaveTerraform {
	public ID Id;
	public string Name;
	public string CivilopediaEntry;
	public int TurnsToComplete;
	public ID RequiredTech;
	public List<string> RequiredResources = [];

	// Optional: a Terrain Improvement associated with the Terraform
	public string Improvement;

	// Lua functions
	public List<string> Validators = [];
	public List<string> Effects = [];
	public string AIScore;

	public MapUnit.AnimatedAction? Animation;

	public string UIAction;
	public string ButtonTexture;

	public void SetUpByTerraformKey(TerraformKey tfKey) {
		Improvement = TerraformKeyToTerrainImprovement(tfKey);
		UIAction = TerraformKeyToUIAction(tfKey);
		ButtonTexture = TerraformKeyToButtonTexture(tfKey);
		Animation = TerraformKeyToAnimation(tfKey);
		LoadLuaFunctions(tfKey);
	}

	private static MapUnit.AnimatedAction? TerraformKeyToAnimation(TerraformKey tfKey) {
		return tfKey switch {
			TerraformKey.BuildRoad => MapUnit.AnimatedAction.ROAD,
			TerraformKey.BuildRailroad => MapUnit.AnimatedAction.ROAD,
			TerraformKey.BuildMine => MapUnit.AnimatedAction.MINE,
			TerraformKey.BuildFortress => MapUnit.AnimatedAction.FORTRESS,
			TerraformKey.BuildBarricade => MapUnit.AnimatedAction.FORTRESS,
			TerraformKey.Irrigate => MapUnit.AnimatedAction.IRRIGATE,
			TerraformKey.ClearWetlands => MapUnit.AnimatedAction.JUNGLE,
			TerraformKey.ClearForest => MapUnit.AnimatedAction.FOREST,
			TerraformKey.PlantForest => MapUnit.AnimatedAction.PLANT,
			_ => null
		};
	}

	private static string TerraformKeyToUIAction(TerraformKey tfKey) {
		return tfKey switch {
			TerraformKey.BuildRoad => C7Action.UnitBuildRoad,
			TerraformKey.BuildRailroad => C7Action.UnitBuildRailroad,
			TerraformKey.BuildMine => C7Action.UnitBuildMine,
			TerraformKey.BuildFortress => C7Action.UnitBuildFortress,
			TerraformKey.ClearDamage => C7Action.UnitClearDamage,
			TerraformKey.BuildAirfield => C7Action.UnitBuildAirfield,
			TerraformKey.BuildRadarTower => C7Action.UnitBuildRadarTower,
			TerraformKey.BuildOutpost => C7Action.UnitBuildOutpost,
			TerraformKey.BuildBarricade => C7Action.UnitBuildBarricade,
			TerraformKey.Irrigate => C7Action.UnitIrrigate,
			TerraformKey.ClearWetlands => C7Action.UnitClearWetlands,
			TerraformKey.ClearForest => C7Action.UnitClearForest,
			TerraformKey.PlantForest => C7Action.UnitPlantForest,
			_ => throw new ArgumentOutOfRangeException(nameof(tfKey), tfKey, null)
		};
	}

	private static string TerraformKeyToButtonTexture(TerraformKey tfKey) {
		return "ui.unit_control." + TerraformKeyToUIAction(tfKey);
	}

	private static string TerraformKeyToTerrainImprovement(TerraformKey tfKey) {
		return tfKey switch {
			TerraformKey.BuildMine => "mine",
			TerraformKey.Irrigate => "irrigation",
			TerraformKey.BuildFortress => "fortress",
			TerraformKey.BuildBarricade => "barricade",
			TerraformKey.BuildRoad => "road",
			TerraformKey.BuildRailroad => "railroad",
			_ => null,
		};
	}

	private void LoadLuaFunctions(TerraformKey tfKey) {
		string actionPath = tfKey switch {
			TerraformKey.BuildMine => "mine",
			TerraformKey.Irrigate => "irrigate",
			TerraformKey.ClearWetlands => "clear_wetlands",
			TerraformKey.ClearForest => "clear_forest",
			_ => null,
		};

		if (actionPath != null)
			Validators.Add($"terraforms.validators.{actionPath}");

		// Add effect
		switch (tfKey) {
			case TerraformKey.ClearWetlands:
			case TerraformKey.ClearForest:
				Effects.Add($"terraforms.effects.{actionPath}");
				break;
		}

		// Add AI-scoring function
		switch (tfKey) {
			case TerraformKey.ClearWetlands:
				AIScore = "terraforms.ai_score.clear_wetlands";
				break;
			case TerraformKey.ClearForest:
				AIScore = "terraforms.ai_score.clear_forest";
				break;
			case TerraformKey.BuildMine:
			case TerraformKey.Irrigate:
				AIScore = "terraforms.ai_score.yield_improvement";
				break;
			case TerraformKey.BuildRoad:
				AIScore = "terraforms.ai_score.road";
				break;
			default:
				AIScore = "terraforms.ai_score.default";
				break;
		}
	}
}
