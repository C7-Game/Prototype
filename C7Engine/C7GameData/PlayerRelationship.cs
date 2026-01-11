using System.Collections.Generic;

namespace C7GameData;

// A class holding all the state of the relationship between two civs.
public class PlayerRelationship {
	public bool atWar = false;

	// p1.playerRelationships[p2].warDeclarationCount is the number of times
	// p2 declared war on p1.
	public int warDeclarationCount = 0;

	// true if a war declaration happened with units inside the player's
	// borders.
	public bool wasSneakAttacked = false;

	// If at war, refuse contact with the relevant player until this turn
	// has been reached.
	public int refuseContactUntilTurn = -1;

	public List<MultiTurnDeal> multiTurnDeals = new List<MultiTurnDeal>();

	public bool declaredWarWithActiveRightOfPassage = false;
}

public class MultiTurnDeal {
	private const int DEFAULT_DEAL_DURATION = 20;
	public DealType dealType { get; private set; }
	public DealSubType dealSubType { get; private set; }
	public DealDetails dealDetails { get; private set; }
	public int goldPerTurn { get; private set; }
	public string resourcePerTurn { get; private set; }
	public int dealDuration { get; private set; }
	public int turnStartDeal { get; private set; }

	public int turnEndDeal { get; private set; }

	// only applicable for Military Alliances and Trade Embargoes
	public ID againstPlayer { get; private set; }

	public MultiTurnDeal(DealType dealType, DealSubType dealSubType, DealDetails dealDetails, int goldPerTurn = 0,
		string resourcePerTurn = null, int dealDuration = DEFAULT_DEAL_DURATION, int turnStartDeal = 0, ID againstPlayer = null) {
		this.dealSubType = dealSubType;
		this.dealType = dealType;
		this.dealDetails = dealDetails;
		this.goldPerTurn = goldPerTurn;
		this.resourcePerTurn = resourcePerTurn;
		this.dealDuration = dealDuration;
		this.turnStartDeal = turnStartDeal;
		// basically peace from the start, don't need to know when it ends
		if (dealSubType == DealSubType.Peace && turnStartDeal == 0)
			this.turnEndDeal = 0;
		else
			this.turnEndDeal = turnStartDeal + dealDuration;
		this.againstPlayer = againstPlayer;
	}

	public int TurnsRemaining(int currentTurn) {
		return turnEndDeal - currentTurn <= 0 ? 0 : turnEndDeal - currentTurn;
	}
}

// https://github.com/maxpetul/C3X/blob/064c8307c5085205c0dc8f2ee5b61ad2c2606523/Civ3Conquests.h#L1399
public enum DealType {
	DiplomaticAgreement,
	Alliance,
	Embargo,
	Map,
	Communication,
	Resource,
	Luxury,
	Gold,
	Technology,
	City,
	Unit,
	None,
}

public enum DealSubType {
	Peace,
	MutualProtectionPact,
	RightOfPassage,
	MilitaryAlliance,
	TradeEmbargo,
	GoldPerTurn,
	ResourcePerTurn,
	LuxuryPerTurn,
	None,
}

public enum DealDetails {
	Inbound,
	Outbound,
	Exchange,
	None,
}
