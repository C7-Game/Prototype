namespace C7GameData {
	// https://forums.civfanatics.com/threads/ai-difficulty-level-bonuses.37490/
	public class Difficulty {
		public ID id;
		public string Name;
		public int NumberOfCitizensBornContent;

		// Only applies to AI; at higher difficulty levels the AI can change
		// governments much faster.
		public int MaxAiGovernmentTransitionTime;

		// If non-zero, the AI gets this number of the best offensive/defensive
		// units the AI can build at the start of the game.
		public int NumberOfAIDefensiveStartingUnits;
		public int NumberOfAIOffensiveStartingUnits;

		// Usually the number of extra settlers
		public int ExtraStartUnit1;

		// Usually the number of extra workers
		public int ExtraStartUnit2;

		// Bonuses for unit support.
		public int AdditionalFreeUnitSupport;
		public int UnitSupportBonusForEachSettlement;

		public int AttackBonusAgainstBarbarians;

		// The cost factor for techs, growth, and production, 10 is a neutral
		// value.
		public int AiCostFactor;
		public int HumanCostFactor = 10;

		public int PercentageOfOptimalCities;

		public int AIToAITradeRate;
		public int CorruptionPercentage;

		// Number of citizens quelled by military.
		//
		// See https://www.civfanatics.com/civ3/strategy/game-mechanics/the-inner-workings-of-resistance-revealed/
		// for details on how this is used - even though it appears to always be
		// 1 in the default game and scenarios, it is moddable.
		public int MilitaryLaw;
	}
}
