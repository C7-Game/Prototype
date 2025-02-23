using System;
using System.Runtime.InteropServices;

namespace QueryCiv3.Biq {
	[StructLayout(LayoutKind.Sequential, Pack = 1)]
	public unsafe struct RACE_City {
		private fixed byte Text[24];
		public string Name { get => Util.GetString(ref this, 0, 24); }
	}

	[StructLayout(LayoutKind.Sequential, Pack = 1)]
	public unsafe struct RACE_LeaderName {
		private fixed byte Text[32];
		public string Name { get => Util.GetString(ref this, 0, 32); }
	}

	[StructLayout(LayoutKind.Sequential, Pack = 1)]
	public unsafe struct RACE_ERAS {
		private fixed byte Text[520];
		public string ForwardFilename { get => Util.GetString(ref this, 0, 260); }
		public string ReverseFilename { get => Util.GetString(ref this, 260, 260); }

	}

	[StructLayout(LayoutKind.Sequential, Pack = 1)]
	public unsafe struct RACE {
		public int Length;
		public int NumberOfCities;

		/*
            Dynamic length gap
            In BIQ files, for each city, 24 bytes of space are used to store that city's name
            Data is instead stored in 2d array of RACECITYNAME
        */

		public int NumberOfGreatLeaders;

		/*
            Dynamic length gap
            In BIQ files, for each great leader, 32 bytes are used to store that leader's name
            Data is instead stored in 2d array of RACELEADERNAME
        */

		private fixed byte Text[208];
		public string LeaderName { get => Util.GetString(ref this, 12, 32); }
		public string LeaderTitle { get => Util.GetString(ref this, 44, 24); }
		public string CivilopediaEntry { get => Util.GetString(ref this, 68, 32); }
		public string Adjective { get => Util.GetString(ref this, 100, 40); }
		public string Name { get => Util.GetString(ref this, 140, 40); }
		public string Noun { get => Util.GetString(ref this, 180, 40); }

		/*
            Dynamic length gap
            In BIQ files, for each era, 520 bytes are used to store that era's forward and reverse filenames
            Data is instead stored in 2d array of RACEERA
        */

		public int CultureGroup;
		public int LeaderGender;
		public int CivilizationGender;
		public int AggressionLevel; // -2 to 2
		public int UniqueCivilizationCounter;
		public int ShunnedGovernment;
		public int FavoriteGovernment;
		public int DefaultColor;
		public int UniqueColor;
		public int FreeTech1;
		public int FreeTech2;
		public int FreeTech3;
		public int FreeTech4;

		private fixed byte Flags[16];
		public bool Militaristic { get => Util.GetFlag(Flags[0], 0); }
		public bool Commercial { get => Util.GetFlag(Flags[0], 1); }
		public bool Expansionist { get => Util.GetFlag(Flags[0], 2); }
		public bool Scientific { get => Util.GetFlag(Flags[0], 3); }
		public bool Religious { get => Util.GetFlag(Flags[0], 4); }
		public bool Industrious { get => Util.GetFlag(Flags[0], 5); }
		public bool Agricultural { get => Util.GetFlag(Flags[0], 6); }
		public bool Seafaring { get => Util.GetFlag(Flags[0], 7); }

		public bool ManageCitizens { get => Util.GetFlag(Flags[4], 0); }
		public bool EmphasizeFood { get => Util.GetFlag(Flags[4], 1); }
		public bool EmphasizeShields { get => Util.GetFlag(Flags[4], 2); }
		public bool EmphasizeTrade { get => Util.GetFlag(Flags[4], 3); }
		public bool ManageProduction { get => Util.GetFlag(Flags[4], 4); }
		public bool NoWonders { get => Util.GetFlag(Flags[4], 5); }
		public bool NoSmallWonders { get => Util.GetFlag(Flags[4], 6); }

		public bool OffensiveLandUnitsNever { get => Util.GetFlag(Flags[8], 0); }
		public bool DefensiveLandUnitsNever { get => Util.GetFlag(Flags[8], 1); }
		public bool ArtilleryLandUnitsNever { get => Util.GetFlag(Flags[8], 2); }
		public bool SettlersNever { get => Util.GetFlag(Flags[8], 3); }
		public bool WorkersNever { get => Util.GetFlag(Flags[8], 4); }
		public bool NavalUnitsNever { get => Util.GetFlag(Flags[8], 5); }
		public bool AirUnitsNever { get => Util.GetFlag(Flags[8], 6); }
		public bool GrowthNever { get => Util.GetFlag(Flags[8], 7); }
		public bool ProductionNever { get => Util.GetFlag(Flags[9], 0); }
		public bool HappinessNever { get => Util.GetFlag(Flags[9], 1); }
		public bool ScienceNever { get => Util.GetFlag(Flags[9], 2); }
		public bool WealthNever { get => Util.GetFlag(Flags[9], 3); }
		public bool TradeNever { get => Util.GetFlag(Flags[9], 4); }
		public bool ExploreNever { get => Util.GetFlag(Flags[9], 5); }
		public bool CultureNever { get => Util.GetFlag(Flags[9], 6); }

		public bool OffensiveLandUnitsOften { get => Util.GetFlag(Flags[12], 0); }
		public bool DefensiveLandUnitsOften { get => Util.GetFlag(Flags[12], 1); }
		public bool ArtilleryLandUnitsOften { get => Util.GetFlag(Flags[12], 2); }
		public bool SettlersOften { get => Util.GetFlag(Flags[12], 3); }
		public bool WorkersOften { get => Util.GetFlag(Flags[12], 4); }
		public bool NavalUnitsOften { get => Util.GetFlag(Flags[12], 5); }
		public bool AirUnitsOften { get => Util.GetFlag(Flags[12], 6); }
		public bool GrowthOften { get => Util.GetFlag(Flags[12], 7); }
		public bool ProductionOften { get => Util.GetFlag(Flags[13], 0); }
		public bool HappinessOften { get => Util.GetFlag(Flags[13], 1); }
		public bool ScienceOften { get => Util.GetFlag(Flags[13], 2); }
		public bool WealthOften { get => Util.GetFlag(Flags[13], 3); }
		public bool TradeOften { get => Util.GetFlag(Flags[13], 4); }
		public bool ExploreOften { get => Util.GetFlag(Flags[13], 5); }
		public bool CultureOften { get => Util.GetFlag(Flags[13], 6); }

		public int Plurality;
		public int UnitTypeForKing;
		public int Flavors;
		private fixed byte UnknownBuffer[4];
		public int DiplomacyTextIndex;
		public int NumberOfScientificLeaders;

		/*
            Dynamic length gap
            In BIQ files, for each scientific leader, 32 bytes are used to store that leader's name
            Data is instead stored in 2d array of RACELEADERNAME
        */
	}
}
