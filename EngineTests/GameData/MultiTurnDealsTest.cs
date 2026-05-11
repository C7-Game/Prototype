using System;
using System.IO;
using C7GameData;
using C7GameData.Save;
using EngineTests.Utils;
using QueryCiv3;
using Xunit;

namespace EngineTests.GameData;

public class MultiTurnDealTest : RemoteSaveLoader {
	private const string SAVES_FOLDER = "saves/multi-turn-deals";
	[Fact]
	public async void TestMultiTurnDeal_Save_A() {
		if (Civ3TestData.ShouldSkipCiv3DependentTests()) {
			return;
		}

		// Save game deal details
		// Round: 47
		// America    -> player-2     (player)
		// England    -> player-3     (AI)

		// Peace                                (America - England)           (0)
		// Right of passage                     (America - England)          (19)
		// Mutual Protection Pact               (America - England)          (20)

		string saveName = "MultiTurnDeal_Save_A.SAV";
		string uri = "https://www.dropbox.com/scl/fi/pb3k02ufgi0q7okwwykvs/MultiTurnDeal_Save_A.SAV?rlkey=y44s3c8czlp01evm3h35sgm1u&st=e6o8e2h0&dl=1";

		(SaveGame game, Exception ex, string savePath) = await LoadGameAndData(saveName, SAVES_FOLDER, uri);

		Assert.Null(ex);
		Assert.NotNull(game);
		Assert.True(File.Exists(savePath));

		// Active peace that has is still active and has never been broken
		Assert.Contains(game.Players[1].playerRelationships["player-3"].multiTurnDeals, d => {
			return d.dealType == DealType.DiplomaticAgreement
				   && d.dealSubType == DealSubType.Peace
				   && d.TurnsRemaining(game.TurnNumber) == 0
				   && d.turnStartDeal == 0
				   && d.turnEndDeal == 0
				   && d.dealDetails == DealDetails.Exchange;
		});
		Assert.Contains(game.Players[2].playerRelationships["player-2"].multiTurnDeals, d => {
			return d.dealType == DealType.DiplomaticAgreement
				   && d.dealSubType == DealSubType.Peace
				   && d.TurnsRemaining(game.TurnNumber) == 0
				   && d.turnStartDeal == 0
				   && d.turnEndDeal == 0
				   && d.dealDetails == DealDetails.Exchange;
		});

		// Active Right of passage agreement
		Assert.Contains(game.Players[1].playerRelationships["player-3"].multiTurnDeals, d => {
			return d.dealType == DealType.DiplomaticAgreement
				   && d.dealSubType == DealSubType.RightOfPassage
				   && d.TurnsRemaining(game.TurnNumber) == 20
				   && d.turnStartDeal == 47
				   && d.turnEndDeal == 67
				   && d.dealDetails == DealDetails.Exchange;
		});
		Assert.Contains(game.Players[2].playerRelationships["player-2"].multiTurnDeals, d => {
			return d.dealType == DealType.DiplomaticAgreement
				   && d.dealSubType == DealSubType.RightOfPassage
				   && d.TurnsRemaining(game.TurnNumber) == 20
				   && d.turnStartDeal == 47
				   && d.turnEndDeal == 67
				   && d.dealDetails == DealDetails.Exchange;
		});

		// Active Mutual Protection Pact
		Assert.Contains(game.Players[1].playerRelationships["player-3"].multiTurnDeals, d => {
			return d.dealType == DealType.DiplomaticAgreement
				   && d.dealSubType == DealSubType.MutualProtectionPact
				   && d.TurnsRemaining(game.TurnNumber) == 19
				   && d.turnStartDeal == 46
				   && d.turnEndDeal == 66
				   && d.dealDetails == DealDetails.Exchange;
		});
		Assert.Contains(game.Players[2].playerRelationships["player-2"].multiTurnDeals, d => {
			return d.dealType == DealType.DiplomaticAgreement
				   && d.dealSubType == DealSubType.MutualProtectionPact
				   && d.TurnsRemaining(game.TurnNumber) == 19
				   && d.turnStartDeal == 46
				   && d.turnEndDeal == 66
				   && d.dealDetails == DealDetails.Exchange;
		});
	}

	[Fact]
	public async void TestMultiTurnDeal_Save_B() {
		if (Civ3TestData.ShouldSkipCiv3DependentTests()) {
			return;
		}

		// Save game deal details
		// Round: 99
		// America    -> player-2     (player)
		// England    -> player-3     (AI)
		// Zulu       -> player-4     (AI)

		// Peace                                (America - England)           (0)
		// Right of passage                     (America - England)           (0)
		// Military Alliance against Zulu       (America - England)          (16)
		// America gives spices to England      (America - England)          (19)

		string saveName = "MultiTurnDeal_Save_B.SAV";
		string uri = "https://www.dropbox.com/scl/fi/aqyjc5ld5qyg5qozq99un/MultiTurnDeal_Save_B.SAV?rlkey=f8y4rf6ufhws5xr65wkohec04&st=mmhybnr3&dl=1";

		(SaveGame game, Exception ex, string savePath) = await LoadGameAndData(saveName, SAVES_FOLDER, uri);

		Assert.Null(ex);
		Assert.NotNull(game);
		Assert.True(File.Exists(savePath));

		// Active peace that has is still active and has never been broken
		Assert.Contains(game.Players[1].playerRelationships["player-3"].multiTurnDeals, d => {
			return d.dealType == DealType.DiplomaticAgreement
				   && d.dealSubType == DealSubType.Peace
				   && d.TurnsRemaining(game.TurnNumber) == 0
				   && d.turnStartDeal == 0
				   && d.turnEndDeal == 0
				   && d.dealDetails == DealDetails.Exchange;
		});
		Assert.Contains(game.Players[2].playerRelationships["player-2"].multiTurnDeals, d => {
			return d.dealType == DealType.DiplomaticAgreement
				   && d.dealSubType == DealSubType.Peace
				   && d.TurnsRemaining(game.TurnNumber) == 0
				   && d.turnStartDeal == 0
				   && d.turnEndDeal == 0
				   && d.dealDetails == DealDetails.Exchange;
		});

		// Active Right of passage agreement that has no more turns remaining but is still active
		Assert.Contains(game.Players[1].playerRelationships["player-3"].multiTurnDeals, d => {
			return d.dealType == DealType.DiplomaticAgreement
				   && d.dealSubType == DealSubType.RightOfPassage
				   && d.TurnsRemaining(game.TurnNumber) == 0
				   && d.turnStartDeal == 78
				   && d.turnEndDeal == 98
				   && d.dealDetails == DealDetails.Exchange;
		});
		Assert.Contains(game.Players[2].playerRelationships["player-2"].multiTurnDeals, d => {
			return d.dealType == DealType.DiplomaticAgreement
				   && d.dealSubType == DealSubType.RightOfPassage
				   && d.TurnsRemaining(game.TurnNumber) == 0
				   && d.turnStartDeal == 78
				   && d.turnEndDeal == 98
				   && d.dealDetails == DealDetails.Exchange;
		});

		// Active Military alliance between player-2 & player-3 against player-4 for 16 more turns, that begun on turn 94
		Assert.Contains(game.Players[1].playerRelationships["player-3"].multiTurnDeals, d => {
			return d.dealType == DealType.Alliance
				   && d.dealSubType == DealSubType.MilitaryAlliance
				   && d.againstPlayer == ID.FromString("player-4")
				   && d.TurnsRemaining(game.TurnNumber) == 16
				   && d.turnStartDeal == 94
				   && d.turnEndDeal == 114
				   && d.dealDetails == DealDetails.Exchange;
		});
		Assert.Contains(game.Players[2].playerRelationships["player-2"].multiTurnDeals, d => {
			return d.dealType == DealType.Alliance
				   && d.dealSubType == DealSubType.MilitaryAlliance
				   && d.againstPlayer == ID.FromString("player-4")
				   && d.TurnsRemaining(game.TurnNumber) == 16
				   && d.turnStartDeal == 94
				   && d.turnEndDeal == 114
				   && d.dealDetails == DealDetails.Exchange;
		});

		// Active luxury deal, player-2 gives player-3 spices
		Assert.Contains(game.Players[1].playerRelationships["player-3"].multiTurnDeals, d => {
			return d.dealType == DealType.Luxury
				   && d.dealSubType == DealSubType.LuxuryPerTurn
				   && d.TurnsRemaining(game.TurnNumber) == 19
				   && d.turnStartDeal == 97
				   && d.turnEndDeal == 117
				   && d.resourcePerTurn == "Spices"
				   && d.dealDetails == DealDetails.Outbound;
		});
		// Active luxury deal, player-3 receives from player-2 spices
		Assert.Contains(game.Players[2].playerRelationships["player-2"].multiTurnDeals, d => {
			return d.dealType == DealType.Luxury
				   && d.dealSubType == DealSubType.LuxuryPerTurn
				   && d.TurnsRemaining(game.TurnNumber) == 19
				   && d.turnStartDeal == 97
				   && d.turnEndDeal == 117
				   && d.resourcePerTurn == "Spices"
				   && d.dealDetails == DealDetails.Inbound;
		});
	}

	[Fact]
	public async void TestMultiTurnDeal_Save_C() {
		if (Civ3TestData.ShouldSkipCiv3DependentTests()) {
			return;
		}

		// Save game deal details
		// Round: 99
		// America    -> player-2     (player)
		// England    -> player-3     (AI)
		// Zulu       -> player-4     (AI)
		// Byzantines -> player-5     (AI)
		// Hittites   -> player-6     (AI)

		// Peace                                (America - England)           (0)
		// Peace                                (America - Zulu)             (20)
		// War                                  (America - Hittites)          (-)
		// Right of passage                     (America - England)           (0)
		// Mutual Protection Pact               (America - England)          (19)
		// America gives spices to England      (America - England)          (19)
		// Byzantines trade embargo with Hittites Against America            (19)

		string saveName = "MultiTurnDeal_Save_C.SAV";
		string uri = "https://www.dropbox.com/scl/fi/chv75f5ezrxzhvclne2sk/MultiTurnDeal_Save_C.SAV?rlkey=dwa8wgzcx03pgjoysqnrauvfc&st=g40d2zta&dl=1";

		(SaveGame game, Exception ex, string savePath) = await LoadGameAndData(saveName, SAVES_FOLDER, uri);

		Assert.Null(ex);
		Assert.NotNull(game);
		Assert.True(File.Exists(savePath));

		// Active peace that has is still active and has never been broken
		Assert.Contains(game.Players[1].playerRelationships["player-3"].multiTurnDeals, d => {
			return d.dealType == DealType.DiplomaticAgreement
				   && d.dealSubType == DealSubType.Peace
				   && d.TurnsRemaining(game.TurnNumber) == 0
				   && d.turnStartDeal == 0
				   && d.turnEndDeal == 0
				   && d.dealDetails == DealDetails.Exchange;
		});
		Assert.Contains(game.Players[2].playerRelationships["player-2"].multiTurnDeals, d => {
			return d.dealType == DealType.DiplomaticAgreement
				   && d.dealSubType == DealSubType.Peace
				   && d.TurnsRemaining(game.TurnNumber) == 0
				   && d.turnStartDeal == 0
				   && d.turnEndDeal == 0
				   && d.dealDetails == DealDetails.Exchange;
		});

		// Active peace that was just signed after war
		Assert.Contains(game.Players[1].playerRelationships["player-4"].multiTurnDeals, d => {
			return d.dealType == DealType.DiplomaticAgreement
				   && d.dealSubType == DealSubType.Peace
				   && d.TurnsRemaining(game.TurnNumber) == 20
				   && d.turnStartDeal == 99
				   && d.turnEndDeal == 119
				   && d.dealDetails == DealDetails.Exchange;
		});
		Assert.Contains(game.Players[3].playerRelationships["player-2"].multiTurnDeals, d => {
			return d.dealType == DealType.DiplomaticAgreement
				   && d.dealSubType == DealSubType.Peace
				   && d.TurnsRemaining(game.TurnNumber) == 20
				   && d.turnStartDeal == 99
				   && d.turnEndDeal == 119
				   && d.dealDetails == DealDetails.Exchange;
		});

		// America at war with Hittites can't have any active multiturn deals
		// But, the relationship is established since they are aware of the existence of each other
		Assert.False(game.Players[1].playerRelationships["player-6"].multiTurnDeals == null);
		Assert.False(game.Players[5].playerRelationships["player-2"].multiTurnDeals == null);
		Assert.True(game.Players[1].playerRelationships["player-6"].multiTurnDeals.Count == 0);
		Assert.True(game.Players[5].playerRelationships["player-2"].multiTurnDeals.Count == 0);

		// Active Right of passage agreement
		Assert.Contains(game.Players[1].playerRelationships["player-3"].multiTurnDeals, d => {
			return d.dealType == DealType.DiplomaticAgreement
				   && d.dealSubType == DealSubType.RightOfPassage
				   && d.TurnsRemaining(game.TurnNumber) == 0
				   && d.turnStartDeal == 79
				   && d.turnEndDeal == 99
				   && d.dealDetails == DealDetails.Exchange;
		});
		Assert.Contains(game.Players[2].playerRelationships["player-2"].multiTurnDeals, d => {
			return d.dealType == DealType.DiplomaticAgreement
				   && d.dealSubType == DealSubType.RightOfPassage
				   && d.TurnsRemaining(game.TurnNumber) == 0
				   && d.turnStartDeal == 79
				   && d.turnEndDeal == 99
				   && d.dealDetails == DealDetails.Exchange;
		});

		// Active Mutual Protection Pact
		Assert.Contains(game.Players[1].playerRelationships["player-3"].multiTurnDeals, d => {
			return d.dealType == DealType.DiplomaticAgreement
				   && d.dealSubType == DealSubType.MutualProtectionPact
				   && d.TurnsRemaining(game.TurnNumber) == 19
				   && d.turnStartDeal == 98
				   && d.turnEndDeal == 118
				   && d.dealDetails == DealDetails.Exchange;
		});
		Assert.Contains(game.Players[2].playerRelationships["player-2"].multiTurnDeals, d => {
			return d.dealType == DealType.DiplomaticAgreement
				   && d.dealSubType == DealSubType.MutualProtectionPact
				   && d.TurnsRemaining(game.TurnNumber) == 19
				   && d.turnStartDeal == 98
				   && d.turnEndDeal == 118
				   && d.dealDetails == DealDetails.Exchange;
		});

		// Active luxury deal, player-2 gives player-3 spices
		Assert.Contains(game.Players[1].playerRelationships["player-3"].multiTurnDeals, d => {
			return d.dealType == DealType.Luxury
				   && d.dealSubType == DealSubType.LuxuryPerTurn
				   && d.TurnsRemaining(game.TurnNumber) == 19
				   && d.turnStartDeal == 98
				   && d.turnEndDeal == 118
				   && d.resourcePerTurn == "Spices"
				   && d.dealDetails == DealDetails.Outbound;
		});
		// Active luxury deal, player-3 receives from player-2 spices
		Assert.Contains(game.Players[2].playerRelationships["player-2"].multiTurnDeals, d => {
			return d.dealType == DealType.Luxury
				   && d.dealSubType == DealSubType.LuxuryPerTurn
				   && d.TurnsRemaining(game.TurnNumber) == 19
				   && d.turnStartDeal == 98
				   && d.turnEndDeal == 118
				   && d.resourcePerTurn == "Spices"
				   && d.dealDetails == DealDetails.Inbound;
		});

		// Active Trade Embargo between Byzantines and Hittites against America
		Assert.Contains(game.Players[4].playerRelationships["player-6"].multiTurnDeals, d => {
			return d.dealType == DealType.Embargo
				   && d.dealSubType == DealSubType.TradeEmbargo
				   && d.TurnsRemaining(game.TurnNumber) == 19
				   && d.turnStartDeal == 98
				   && d.turnEndDeal == 118
				   && d.dealDetails == DealDetails.Exchange;
		});
		Assert.Contains(game.Players[5].playerRelationships["player-5"].multiTurnDeals, d => {
			return d.dealType == DealType.Embargo
				   && d.dealSubType == DealSubType.TradeEmbargo
				   && d.TurnsRemaining(game.TurnNumber) == 19
				   && d.turnStartDeal == 98
				   && d.turnEndDeal == 118
				   && d.dealDetails == DealDetails.Exchange;
		});
	}

	[Fact]
	public async void TestMultiTurnDeal_Save_D() {
		if (Civ3TestData.ShouldSkipCiv3DependentTests()) {
			return;
		}

		// Save game deal details
		// Round: 98
		// America    -> player-2     (player)
		// England    -> player-3     (AI)
		// Zulu       -> player-4     (AI)
		// Hittites   -> player-6     (AI)

		// Peace                                (America - England)           (0)
		// Right of passage                     (America - England)           (0)
		// Military Alliance against Zulu       (America - England)          (16)
		// Military Alliance against Hittites   (America - England)          (20)

		string saveName = "MultiTurnDeal_Save_D.SAV";
		string uri = "https://www.dropbox.com/scl/fi/1zpkjmvgobctndfwdml5z/MultiTurnDeal_Save_D.SAV?rlkey=d2v15p7a05s0nslnj9nz66wwr&st=iaajcftt&dl=1";

		(SaveGame game, Exception ex, string savePath) = await LoadGameAndData(saveName, SAVES_FOLDER, uri);

		Assert.Null(ex);
		Assert.NotNull(game);
		Assert.True(File.Exists(savePath));

		// Active peace that has is still active and has never been broken
		Assert.Contains(game.Players[1].playerRelationships["player-3"].multiTurnDeals, d => {
			return d.dealType == DealType.DiplomaticAgreement
				   && d.dealSubType == DealSubType.Peace
				   && d.TurnsRemaining(game.TurnNumber) == 0
				   && d.turnStartDeal == 0
				   && d.turnEndDeal == 0
				   && d.dealDetails == DealDetails.Exchange;
		});
		Assert.Contains(game.Players[2].playerRelationships["player-2"].multiTurnDeals, d => {
			return d.dealType == DealType.DiplomaticAgreement
				   && d.dealSubType == DealSubType.Peace
				   && d.TurnsRemaining(game.TurnNumber) == 0
				   && d.turnStartDeal == 0
				   && d.turnEndDeal == 0
				   && d.dealDetails == DealDetails.Exchange;
		});

		// Active Right of passage agreement that has no more turns remaining but is still active
		Assert.Contains(game.Players[1].playerRelationships["player-3"].multiTurnDeals, d => {
			return d.dealType == DealType.DiplomaticAgreement
				   && d.dealSubType == DealSubType.RightOfPassage
				   && d.TurnsRemaining(game.TurnNumber) == 0
				   && d.turnStartDeal == 78
				   && d.turnEndDeal == 98
				   && d.dealDetails == DealDetails.Exchange;
		});
		Assert.Contains(game.Players[2].playerRelationships["player-2"].multiTurnDeals, d => {
			return d.dealType == DealType.DiplomaticAgreement
				   && d.dealSubType == DealSubType.RightOfPassage
				   && d.TurnsRemaining(game.TurnNumber) == 0
				   && d.turnStartDeal == 78
				   && d.turnEndDeal == 98
				   && d.dealDetails == DealDetails.Exchange;
		});

		// Active Military alliance between player-2 & player-3 against player-4 for 16 more turns, that begun on turn 94
		Assert.Contains(game.Players[1].playerRelationships["player-3"].multiTurnDeals, d => {
			return d.dealType == DealType.Alliance
				   && d.dealSubType == DealSubType.MilitaryAlliance
				   && d.againstPlayer == ID.FromString("player-4")
				   && d.TurnsRemaining(game.TurnNumber) == 16
				   && d.turnStartDeal == 94
				   && d.turnEndDeal == 114
				   && d.dealDetails == DealDetails.Exchange;
		});
		Assert.Contains(game.Players[2].playerRelationships["player-2"].multiTurnDeals, d => {
			return d.dealType == DealType.Alliance
				   && d.dealSubType == DealSubType.MilitaryAlliance
				   && d.againstPlayer == ID.FromString("player-4")
				   && d.TurnsRemaining(game.TurnNumber) == 16
				   && d.turnStartDeal == 94
				   && d.turnEndDeal == 114
				   && d.dealDetails == DealDetails.Exchange;
		});

		// Active Military alliance between player-2 & player-3 against player-6 for 20 more turns, that begun on turn 98
		Assert.Contains(game.Players[1].playerRelationships["player-3"].multiTurnDeals, d => {
			return d.dealType == DealType.Alliance
				   && d.dealSubType == DealSubType.MilitaryAlliance
				   && d.againstPlayer == ID.FromString("player-6")
				   && d.TurnsRemaining(game.TurnNumber) == 20
				   && d.turnStartDeal == 98
				   && d.turnEndDeal == 118
				   && d.dealDetails == DealDetails.Exchange;
		});
		Assert.Contains(game.Players[2].playerRelationships["player-2"].multiTurnDeals, d => {
			return d.dealType == DealType.Alliance
				   && d.dealSubType == DealSubType.MilitaryAlliance
				   && d.againstPlayer == ID.FromString("player-6")
				   && d.TurnsRemaining(game.TurnNumber) == 20
				   && d.turnStartDeal == 98
				   && d.turnEndDeal == 118
				   && d.dealDetails == DealDetails.Exchange;
		});

	}

	[Fact]
	public async void TestMultiTurnDeal_Save_E() {
		if (Civ3TestData.ShouldSkipCiv3DependentTests()) {
			return;
		}

		// Save game deal details
		// Round: 0
		// America    -> player-2     (player)
		// Sweden    -> player-10     (AI)


		// Peace                                     (Ott Empire - Sweden)          (0)
		// Ott Empire gives Wines to Sweden          (Ott Empire - Sweden)          (20)
		// Ott Empire gives Dyes to Sweden           (Ott Empire - Sweden)          (20)
		// Ott Empire gives Incense to Sweden        (Ott Empire - Sweden)          (20)
		// Ott Empire gives Horses to Sweden         (Ott Empire - Sweden)          (20)
		// Sweden gives Furs to Ott Empire           (Ott Empire - Sweden)          (20)

		string scenarioBiqPath = Path.Combine(Civ3Location.GetCiv3Path(), "Conquests", "Scenarios", "8 MP Napoleonic Europe.biq");
		string scenarioPediaPath = Path.Combine(Civ3Location.GetCiv3Path(), "Conquests", "Conquests", "Napoleonic Europe", "Text", "PediaIcons.txt");
		string saveName = "MultiTurnDeal_Save_E.SAV";
		string uri = "https://www.dropbox.com/scl/fi/8uutphldi1wzn59qd8h29/MultiTurnDeal_Save_E.SAV?rlkey=q0tay21soe6g0aefqpmshbeq7&st=y3anm45t&dl=1";

		(SaveGame game, Exception ex, string savePath) = await LoadGameAndData(saveName, SAVES_FOLDER, uri, scenarioBiqPath, scenarioPediaPath);

		Assert.Null(ex);
		Assert.NotNull(game);
		Assert.True(File.Exists(savePath));

		// Active peace that has is still active and has never been broken
		Assert.Contains(game.Players[1].playerRelationships["player-10"].multiTurnDeals, d => {
			return d.dealType == DealType.DiplomaticAgreement
				   && d.dealSubType == DealSubType.Peace
				   && d.TurnsRemaining(game.TurnNumber) == 0
				   && d.turnStartDeal == 0
				   && d.turnEndDeal == 0
				   && d.dealDetails == DealDetails.Exchange;
		});
		Assert.Contains(game.Players[9].playerRelationships["player-2"].multiTurnDeals, d => {
			return d.dealType == DealType.DiplomaticAgreement
				   && d.dealSubType == DealSubType.Peace
				   && d.TurnsRemaining(game.TurnNumber) == 0
				   && d.turnStartDeal == 0
				   && d.turnEndDeal == 0
				   && d.dealDetails == DealDetails.Exchange;
		});

		// Active luxury deal, player-2 gives player-10 Wines
		Assert.Contains(game.Players[1].playerRelationships["player-10"].multiTurnDeals, d => {
			return d.dealType == DealType.Luxury
				   && d.dealSubType == DealSubType.LuxuryPerTurn
				   && d.TurnsRemaining(game.TurnNumber) == 20
				   && d.turnStartDeal == 0
				   && d.turnEndDeal == 20
				   && d.resourcePerTurn == "Wines"
				   && d.dealDetails == DealDetails.Outbound;
		});
		// Active luxury deal, player-10 receives Wines from player-2
		Assert.Contains(game.Players[9].playerRelationships["player-2"].multiTurnDeals, d => {
			return d.dealType == DealType.Luxury
				   && d.dealSubType == DealSubType.LuxuryPerTurn
				   && d.TurnsRemaining(game.TurnNumber) == 20
				   && d.turnStartDeal == 0
				   && d.turnEndDeal == 20
				   && d.resourcePerTurn == "Wines"
				   && d.dealDetails == DealDetails.Inbound;
		});

		// Active luxury deal, player-2 gives player-10 Dyes
		Assert.Contains(game.Players[1].playerRelationships["player-10"].multiTurnDeals, d => {
			return d.dealType == DealType.Luxury
				   && d.dealSubType == DealSubType.LuxuryPerTurn
				   && d.TurnsRemaining(game.TurnNumber) == 20
				   && d.turnStartDeal == 0
				   && d.turnEndDeal == 20
				   && d.resourcePerTurn == "Dyes"
				   && d.dealDetails == DealDetails.Outbound;
		});
		// Active luxury deal, player-10 receives Dyes from player-2
		Assert.Contains(game.Players[9].playerRelationships["player-2"].multiTurnDeals, d => {
			return d.dealType == DealType.Luxury
				   && d.dealSubType == DealSubType.LuxuryPerTurn
				   && d.TurnsRemaining(game.TurnNumber) == 20
				   && d.turnStartDeal == 0
				   && d.turnEndDeal == 20
				   && d.resourcePerTurn == "Dyes"
				   && d.dealDetails == DealDetails.Inbound;
		});

		// Active luxury deal, player-2 gives player-10 Incense
		Assert.Contains(game.Players[1].playerRelationships["player-10"].multiTurnDeals, d => {
			return d.dealType == DealType.Luxury
				   && d.dealSubType == DealSubType.LuxuryPerTurn
				   && d.TurnsRemaining(game.TurnNumber) == 20
				   && d.turnStartDeal == 0
				   && d.turnEndDeal == 20
				   && d.resourcePerTurn == "Incense"
				   && d.dealDetails == DealDetails.Outbound;
		});
		// Active luxury deal, player-10 receives Incense from player-2
		Assert.Contains(game.Players[9].playerRelationships["player-2"].multiTurnDeals, d => {
			return d.dealType == DealType.Luxury
				   && d.dealSubType == DealSubType.LuxuryPerTurn
				   && d.TurnsRemaining(game.TurnNumber) == 20
				   && d.turnStartDeal == 0
				   && d.turnEndDeal == 20
				   && d.resourcePerTurn == "Incense"
				   && d.dealDetails == DealDetails.Inbound;
		});

		// Active luxury deal, player-10 gives player-2 Furs
		Assert.Contains(game.Players[1].playerRelationships["player-10"].multiTurnDeals, d => {
			return d.dealType == DealType.Luxury
				   && d.dealSubType == DealSubType.LuxuryPerTurn
				   && d.TurnsRemaining(game.TurnNumber) == 20
				   && d.turnStartDeal == 0
				   && d.turnEndDeal == 20
				   && d.resourcePerTurn == "Furs"
				   && d.dealDetails == DealDetails.Inbound;
		});
		// Active luxury deal, player-2 receives Furs from player-10
		Assert.Contains(game.Players[9].playerRelationships["player-2"].multiTurnDeals, d => {
			return d.dealType == DealType.Luxury
				   && d.dealSubType == DealSubType.LuxuryPerTurn
				   && d.TurnsRemaining(game.TurnNumber) == 20
				   && d.turnStartDeal == 0
				   && d.turnEndDeal == 20
				   && d.resourcePerTurn == "Furs"
				   && d.dealDetails == DealDetails.Outbound;
		});

		// Active resource deal, player-2 gives player-10 Horses
		Assert.Contains(game.Players[1].playerRelationships["player-10"].multiTurnDeals, d => {
			return d.dealType == DealType.Resource
				   && d.dealSubType == DealSubType.ResourcePerTurn
				   && d.TurnsRemaining(game.TurnNumber) == 20
				   && d.turnStartDeal == 0
				   && d.turnEndDeal == 20
				   && d.resourcePerTurn == "Horses"
				   && d.dealDetails == DealDetails.Outbound;
		});
		// Active resource deal, player-10 receives Horses from player-2
		Assert.Contains(game.Players[9].playerRelationships["player-2"].multiTurnDeals, d => {
			return d.dealType == DealType.Resource
				   && d.dealSubType == DealSubType.ResourcePerTurn
				   && d.TurnsRemaining(game.TurnNumber) == 20
				   && d.turnStartDeal == 0
				   && d.turnEndDeal == 20
				   && d.resourcePerTurn == "Horses"
				   && d.dealDetails == DealDetails.Inbound;
		});
	}

	[Fact]
	public async void TestMultiTurnDeal_Save_F() {
		if (Civ3TestData.ShouldSkipCiv3DependentTests()) {
			return;
		}

		// Save game deal details
		// Round: 1
		// America      -> player-2      (player)
		// Portugal     -> player-12     (AI)


		// Same as Save_E, one turn later, plus
		// Peace                                                 (Ott Empire - Portugal)          (0)
		// Ott Empire gives 1 Gold per turn to Portugal          (Ott Empire - Portugal)          (20)

		string scenarioBiqPath = Path.Combine(Civ3Location.GetCiv3Path(), "Conquests", "Scenarios", "8 MP Napoleonic Europe.biq");
		string scenarioPediaPath = Path.Combine(Civ3Location.GetCiv3Path(), "Conquests", "Conquests", "Napoleonic Europe", "Text", "PediaIcons.txt");
		string saveName = "MultiTurnDeal_Save_F.SAV";
		string uri = "https://www.dropbox.com/scl/fi/l8bm8dhacd85cwyxn3nn7/MultiTurnDeal_Save_F.SAV?rlkey=mr7itejsucesk14h859jwjffu&st=fuo8n4d9&dl=1";

		(SaveGame game, Exception ex, string savePath) = await LoadGameAndData(saveName, SAVES_FOLDER, uri, scenarioBiqPath, scenarioPediaPath);

		Assert.Null(ex);
		Assert.NotNull(game);
		Assert.True(File.Exists(savePath));

		// Active peace that has is still active and has never been broken
		Assert.Contains(game.Players[1].playerRelationships["player-12"].multiTurnDeals, d => {
			return d.dealType == DealType.DiplomaticAgreement
				   && d.dealSubType == DealSubType.Peace
				   && d.TurnsRemaining(game.TurnNumber) == 0
				   && d.turnStartDeal == 0
				   && d.turnEndDeal == 0
				   && d.dealDetails == DealDetails.Exchange;
		});
		Assert.Contains(game.Players[11].playerRelationships["player-2"].multiTurnDeals, d => {
			return d.dealType == DealType.DiplomaticAgreement
				   && d.dealSubType == DealSubType.Peace
				   && d.TurnsRemaining(game.TurnNumber) == 0
				   && d.turnStartDeal == 0
				   && d.turnEndDeal == 0
				   && d.dealDetails == DealDetails.Exchange;
		});

		// Active gold per turn deal, player-2 gives player-12 1 GPT
		Assert.Contains(game.Players[1].playerRelationships["player-12"].multiTurnDeals, d => {
			return d.dealType == DealType.Gold
				   && d.dealSubType == DealSubType.GoldPerTurn
				   && d.TurnsRemaining(game.TurnNumber) == 20
				   && d.turnStartDeal == 1
				   && d.turnEndDeal == 21
				   && d.resourcePerTurn == null
				   && d.goldPerTurn == 1
				   && d.dealDetails == DealDetails.Outbound;
		});
		// Active gold per turn deal, player-12 receives 1 GPT from player-2
		Assert.Contains(game.Players[11].playerRelationships["player-2"].multiTurnDeals, d => {
			return d.dealType == DealType.Gold
				   && d.dealSubType == DealSubType.GoldPerTurn
				   && d.TurnsRemaining(game.TurnNumber) == 20
				   && d.turnStartDeal == 1
				   && d.turnEndDeal == 21
				   && d.resourcePerTurn == null
				   && d.goldPerTurn == 1
				   && d.dealDetails == DealDetails.Inbound;
		});
	}
}
