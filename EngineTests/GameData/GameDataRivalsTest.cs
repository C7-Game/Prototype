using System.Collections.Generic;
using C7GameData;
using Xunit;

namespace EngineTests.GameData;

public class GameDataRivalsTest {

	[Fact]
	public void GetRivals_ExcludesSelfBarbariansAndDefeatedPlayers() {
		Player self = new() { civilization = new Civilization("Rome") };
		Player alive = new() { civilization = new Civilization("Greece") };
		Player defeated = new() { civilization = new Civilization("Egypt"), defeated = true };
		Player barbarians = new() { civilization = new Civilization("Barbarians") { isBarbarian = true } };

		C7GameData.GameData gameData = new();
		gameData.players.AddRange(new List<Player> { self, alive, defeated, barbarians });

		List<Player> rivals = gameData.GetRivals(self);

		Assert.Single(rivals);
		Assert.Same(alive, rivals[0]);
	}

	[Fact]
	public void GetKnownRivals_OnlyIncludesRivalsWithAnEstablishedRelationship() {
		Player self = new() { civilization = new Civilization("Rome") };
		Player metRival = new() { civilization = new Civilization("Greece"), id = ID.FromString("player-2") };
		Player unmetRival = new() { civilization = new Civilization("Egypt"), id = ID.FromString("player-3") };

		self.playerRelationships[metRival.id] = new PlayerRelationship();
		// Note: unmetRival is a valid rival by GetRivals' rules, but self has
		// no relationship entry for them yet (never encountered).

		C7GameData.GameData gameData = new();
		gameData.players.AddRange(new List<Player> { self, metRival, unmetRival });

		List<Player> knownRivals = gameData.GetKnownRivals(self);

		Assert.Single(knownRivals);
		Assert.Same(metRival, knownRivals[0]);
	}

	[Fact]
	public void GetKnownRivals_EmptyWhenNoRelationshipsEstablished() {
		Player self = new() { civilization = new Civilization("Rome"), id = ID.FromString("player-2") };
		Player rival = new() { civilization = new Civilization("Greece"), id = ID.FromString("player-3") };

		C7GameData.GameData gameData = new();
		gameData.players.AddRange(new List<Player> { self, rival });

		List<Player> knownRivals = gameData.GetKnownRivals(self);

		Assert.Empty(knownRivals);
	}
}
