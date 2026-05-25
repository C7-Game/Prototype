using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using C7Engine.Lua;
using C7GameData;
using C7GameData.Save;
using EngineTests.Utils;
using Xunit;

namespace EngineTests.GameData;

public class MapUnitCombatFacingTest {
	[Fact]
	public void UnitsTurnTowardTargetWhenRotateBeforeAttackIsDisabled() {
		MapUnit unit = MakeUnit(rotateBeforeAttack: false);

		Assert.Equal(TileDirection.EAST, unit.GetAttackAnimationDirection(TileDirection.EAST));
		Assert.Equal(TileDirection.WEST, unit.GetDefenseAnimationDirection(TileDirection.EAST));
	}

	[Fact]
	public void UnitsUseBroadsideFacingWhenRotateBeforeAttackIsEnabled() {
		MapUnit unit = MakeUnit(rotateBeforeAttack: true);

		Assert.Equal(TileDirection.NORTH, unit.GetAttackAnimationDirection(TileDirection.EAST));
		Assert.Equal(TileDirection.SOUTH, unit.GetDefenseAnimationDirection(TileDirection.EAST));
	}

	[Fact]
	public void UnitPrototypePreservesRotateBeforeAttackFromSavedPrototype() {
		UnitPrototype unit = new(new() {
			name = "Galley",
			flags = [SaveUnitPrototype.Flag.RotateBeforeAttack],
		}, []);

		Assert.True(unit.rotateBeforeAttack);
		Assert.Contains(SaveUnitPrototype.Flag.RotateBeforeAttack, unit.flags);
	}

	[Fact]
	public void BaseRulesetMarksBroadsideUnitsAsRotateBeforeAttack() {
		string[] expectedRotatingUnits = [
			"Radar Artillery",
			"Galley",
			"Caravel",
			"Frigate",
			"Galleon",
			"Battleship",
			"Man-O-War",
			"Privateer",
			"Carrack",
			"Curragh",
		];

		using JsonDocument ruleset = JsonUtils.LoadBaseRuleset();
		JsonElement unitPrototypes = ruleset.RootElement.GetProperty("unitPrototypes");
		foreach (JsonElement unitPrototype in unitPrototypes.EnumerateArray()) {
			string unitName = unitPrototype.GetProperty("name").GetString();
			Assert.False(unitPrototype.TryGetProperty("rotateBeforeAttack", out _), $"{unitName} should use flags for unit abilities.");
		}

		HashSet<string> rotatingUnits = unitPrototypes.EnumerateArray()
			.Where(unitPrototype => UnitFlags(unitPrototype).Contains("rotateBeforeAttack"))
			.Select(unitPrototype => unitPrototype.GetProperty("name").GetString())
			.ToHashSet();

		Assert.Equal(expectedRotatingUnits.OrderBy(unitName => unitName), rotatingUnits.OrderBy(unitName => unitName));
	}

	private static HashSet<string> UnitFlags(JsonElement unitPrototype) {
		if (!unitPrototype.TryGetProperty("flags", out JsonElement flags)) {
			return [];
		}

		return flags.EnumerateArray()
			.Select(flag => flag.GetString())
			.ToHashSet();
	}

	private static MapUnit MakeUnit(bool rotateBeforeAttack) {
		return new(ID.None("unit")) {
			unitType = new() {
				rotateBeforeAttack = rotateBeforeAttack,
			}
		};
	}
}
