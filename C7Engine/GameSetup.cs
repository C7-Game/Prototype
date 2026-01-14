using System;
using System.Collections.Generic;
using C7GameData;
using C7GameData.Save;
using Serilog;

namespace C7Engine;

public struct SelectedOpponent {
	public bool isRandom;
	public string Name;
}

public class GameSetup {
	private static ILogger log = Log.ForContext<GameSetup>();

	public Civilization civilization;
	public Difficulty difficulty;

	ID.Factory ids;

	public void Populate(SaveGame save, WorldCharacteristics WorldCharacteristics, List<SelectedOpponent> opponents) {
		log.Information("Starting map generation");
		save.Map = new SaveMap(MapGenerator.GenerateMap(WorldCharacteristics));
		save.Seed = WorldCharacteristics.mapSeed;
		log.Information("Done with map generation");
		Random rand = new(WorldCharacteristics.mapSeed + 0x531);
		ids = new(save);

		// Hack: reuse the save but clear out the non-barbarian players.
		//
		// Longer term we'll need to split out our own
		// "conquests.bic" type file and load that - until then we'll use this
		// hack of reusing the static save.
		//
		// Start at index 1 to skip the barbarians.
		save.Players.RemoveRange(1, save.Players.Count - 1);

		// Clear out the units, we'll add new ones.
		save.Units.Clear();

		// Add the human player.
		AddPlayer(save, this.civilization,
					WorldCharacteristics.defaultGovernment.id,
					isHuman: true);

		// Add the opponents.
		HashSet<string> taken = new();
		taken.Add(this.civilization.name);

		foreach (SelectedOpponent opponent in opponents) {
			bool isRandom = opponent.isRandom;
			string selectedName = opponent.Name;

			if (taken.Contains(opponent.Name)) {
				isRandom = true;
			}

			if (isRandom) {
				do {
					selectedName = save.Civilizations[rand.Next(1, save.Civilizations.Count)].name;
				} while (taken.Contains(selectedName));
			}
			taken.Add(selectedName);

			Civilization civ = save.Civilizations.Find(x => x.name == selectedName);
			AddPlayer(save, civ,
						WorldCharacteristics.defaultGovernment.id,
						isHuman: false);
		}

		save.GameDifficulty = difficulty;
	}

	private void AddPlayer(SaveGame save, Civilization civ, ID defaultGovernment, bool isHuman) {
		SavePlayer player = new() {
			human = isHuman,
			id = ids.CreateID("Player"),
			colorIndex = civ.colorIndex,
			civilization = civ.name,
			knownTechs = civ.startingTechs,
			// TODO: stop hardcoding this
			eraCivilopediaName = "ERAS_Ancient_Times",
			// TODO: load this from the rules
			gold = 10,
			governmentId = defaultGovernment,
		};
		save.Players.Add(player);

		SaveTile startingTile = save.Map.startingLocations[save.Players.Count - 2];
		TileLocation startingLocation = new TileLocation(startingTile.X, startingTile.Y);

		AddUnit(save, player, save.Rules.StartUnitType1, startingLocation);
		AddUnit(save, player, save.Rules.StartUnitType2, startingLocation);
		if (civ.traits.Contains(Civilization.Trait.Expansionist)) {
			AddUnit(save, player, save.Rules.ScoutUnitType, startingLocation);
		}
	}

	private void AddUnit(SaveGame save, SavePlayer player, string unitType, TileLocation location) {
		SaveUnit unit = new() {
			id = ids.CreateID(unitType),
			prototype = unitType,
			owner = player.id,
			previousLocation = new TileLocation(-1, -1),
			currentLocation = location,

			// TODO: stop hardcoding these
			hitPointsRemaining = 3,
			movePointsRemaining = 1,
			experience = "Regular",

			facingDirection = TileDirection.NORTH,
		};
		save.Units.Add(unit);
	}
}
