using System;
using System.Runtime.InteropServices;

namespace QueryCiv3.Biq {
	[StructLayout(LayoutKind.Sequential, Pack = 1)]
	public unsafe struct RULE_CULT {
		private fixed byte Text[64];
		public string Name { get => Util.GetString(ref this, 0, 64); }
	}

	[StructLayout(LayoutKind.Sequential, Pack = 1)]
	public unsafe struct RULE {
		public int Length;

		private fixed byte Text[96];
		public string CitySizeLevel1Name { get => Util.GetString(ref this, 4, 32); }
		public string CitySizeLevel2Name { get => Util.GetString(ref this, 36, 32); }
		public string CitySizeLevel3Name { get => Util.GetString(ref this, 68, 32); }

		public int NumberOfSpaceshipParts;

		/*
            Dynamic length gap
            In BIQ files, for each spaceship part, there is 1 int specifying the quantity of that part needed
            Data is instead stored in 2d array RuleSpaceship
        */

		public int AdvancedBarbarianUnitType;
		public int BasicBarbarianUnitType;
		public int BarbarianSeaUnitType;
		public int CitiesNeededToSupportAnArmy;
		public int ChanceOfRioting;
		public int TurnPenaltyForEachDraftedCitizen;
		public int ShieldsCostPerGold;
		public int FortressDefensiveBonus;
		public int CitizensAffectedByEachHappyFace;
		private fixed byte UnknownBuffer[8];
		public int ForestValueInShields;
		public int ShieldValueInGold;
		public int CitizenValueInShields;
		public int DefaultDifficultyLevel;
		public int BattleCreatedUnit;
		public int BuildArmyUnit;
		public int BuildingDefensiveBonus;
		public int CitizenDefensiveBonus;
		public int DefaultMoneyResource;
		public int ChanceToInterceptEnemyAirMissions;
		public int ChanceToInterceptEnemyStealthMissions;
		public int StartingTreasury;
		private fixed byte UnknownBuffer2[4];
		public int FoodConsumptionPerCitizen;
		public int RiverDefensiveBonus;
		public int TurnPenaltyForEachHurrySacrifice;
		public int Scout;
		public int Slave;
		public int MovementAlongRoads;
		public int StartUnitType1;
		public int StartUnitType2;
		public int MinimumPopulationForWeLoveTheKing;
		public int TownDefenseBonus;
		public int CityDefenseBonus;
		public int MetropolisDefenseBonus;
		public int MaximumLevel1CitySize;
		public int MaximumLevel2CitySize;
		private fixed byte UnknownBuffer3[4];
		public int FortificationsDefensiveBonus;
		public int NumberOfCultureLevels;

		/*
            Dynamic length gap
            In BIQ files, for each culture level, there are 64 bytes for a culture level name string
            Data is instead stored in 2d array of RULECULT
        */

		public int BorderExpansionMultiplier;
		public int BorderFactor;
		public int FutureTechCost;
		public int GoldenAgeDuration;
		public int MaximumResearchTime;
		public int MinimumResearchTime;
		public int FlagUnitType;
		public int UpgradeCost;
	}
}
