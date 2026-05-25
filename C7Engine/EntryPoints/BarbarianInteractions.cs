using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using C7GameData;

namespace C7Engine;

public class BarbarianInteractions {
	public static int SpawnBarbarians(GameData gameData) {
		Player barbPlayer = gameData.players.Find(player => player.isBarbarians);
		var activity = gameData.barbarianInfo.barbarianActivity;

		if (activity == BarbarianActivity.None)
			return 0;

		// A random number of camps will spawn a unit each turn.
		var spawnRate = DetermineSpawnRate(activity);
		var spawnMeasure = spawnRate * GameData.rng.Next(gameData.map.barbarianCamps.Count);
		int barbariansToSpawn = (int)Math.Ceiling(spawnMeasure);

		// Make the spawn locations random by shuffling a list of camp indexes
		List<int> tileIndicies = Enumerable.Range(0, gameData.map.barbarianCamps.Count).ToList();
		GameData.rng.Shuffle<int>(CollectionsMarshal.AsSpan(tileIndicies));

		// Sample barbarian camps
		var spawningCamps = tileIndicies
			.Select(i => gameData.map.barbarianCamps[i])
			.Take(barbariansToSpawn);

		// Spawn a unit
		foreach (Tile camp in spawningCamps) {
			UnitPrototype unitType = SelectBarbarianUnitType(gameData.barbarianInfo, camp);
			Tile tile = SelectSpawnTile(barbPlayer, camp, unitType);
			if (tile != null) {
				gameData.SpawnUnit(barbPlayer, unitType, tile);
			}
		}

		return barbariansToSpawn;
	}

	/// <summary>
	/// Apply barbarian activity level to barbarian unit spawn rate. Currently NOT based on Civ3 values.
	/// TODO: Make configurable
	/// TODO: Determine what these values are in Civ3 (could be a constant across all) 
	/// </summary>
	private static float DetermineSpawnRate(BarbarianActivity activity) {
		switch (activity) {
			case BarbarianActivity.None:
				return 0;
			case BarbarianActivity.Sedentary:
				return 0.03f;
			case BarbarianActivity.Roaming:
				return 0.05f;
			case BarbarianActivity.Restless:
				return 0.08f;
			case BarbarianActivity.Raging:
				return 0.12f;
			default:
				throw new ArgumentOutOfRangeException(nameof(activity), activity, null);
		}

	}

	public static UnitPrototype SelectBarbarianUnitType(BarbarianInfo barbInfo, Tile tile) {
		// Coastal camps have a 20% chance of spawning a sea unit
		if (tile.NeighborsWater() && GameData.rng.Next(100) < 20) {
			return barbInfo.barbarianSeaUnitProto;
		}

		// Land units are generated in a 3:1 ratio, three advanced units for every basic barbarian
		return GameData.rng.Next(100) < 25 ? barbInfo.advancedBarbarian : barbInfo.basicBarbarian;
	}

	public static Tile SelectSpawnTile(Player player, Tile camp, UnitPrototype unitType) {
		// Spawn land units at the camp
		if (unitType.IsLandUnit())
			return camp;

		// Spawn sea units on a coast tile, but not in a lake or on a tile occupied by another player
		bool CanSpawnSeaUnits(Tile t) =>
			t.IsCoast() && !t.isFreshWater && t.unitsOnTile.TrueForAll(u => u.owner == player);

		if (unitType.IsSeaUnit()) {
			// Check first two ranks around camp for a suitable tile, or bail with a null 
			return camp.FindInRing(1, CanSpawnSeaUnits)
				?? camp.FindInRing(2, CanSpawnSeaUnits);
		}

		// Default to camp 
		return camp;
	}
}
