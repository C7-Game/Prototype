using System;
using System.Runtime.InteropServices;

namespace QueryCiv3.Sav {
	[StructLayout(LayoutKind.Sequential, Pack = 1)]
	public unsafe struct LEAD_LEAD {
		private fixed byte UnknownBuffer[4];
		public int CancelledDeal;
		private fixed byte UnknownBuffer2[8];
		public int CaughtSpy;
		private fixed byte UnknownBuffer3[4];
		public int Tribute;
		private fixed byte UnknownBuffer4[8];
		public int Gift;
		private fixed byte UnknownBuffer5[4];
		public int ICBM;
		public int ICBMOther;
		private fixed byte UnknownBuffer6[24];
	}

	[StructLayout(LayoutKind.Sequential, Pack = 1)]
	public unsafe struct CULT {
		private fixed byte HeaderText[4];
		public int Length;
		private fixed byte UnknownBuffer[4];
		public int TotalCulture;
		public int CultureGainedLastTurn;
		private fixed byte UnknownBuffer2[4];
	}

	[StructLayout(LayoutKind.Sequential, Pack = 1)]
	public unsafe struct ESPN {
		private fixed byte HeaderText[4];
		public int Length;
		private fixed byte UnknownBuffer[32];
	}

	[StructLayout(LayoutKind.Sequential, Pack = 1)]
	public unsafe struct LEAD_LEAD_Diplomacy {
		public int EntryType;
		public int Data1;
		public int Data2;
	}

	[StructLayout(LayoutKind.Sequential, Pack = 1)]
	public unsafe struct LEAD_GOOD_LEAD {
		public bool HasResource;
		public bool ImportExport;
		public bool Tradeable;
	}

	[StructLayout(LayoutKind.Sequential, Pack = 1)]
	public unsafe struct LEAD {
		private fixed byte HeaderText[4];
		public int Length;
		public int PlayerID;
		public int RaceID;
		private fixed byte UnknownBuffer[4];
		public int Power;
		public int CapitalCity;
		public int Difficulty;
		private fixed byte UnknownBuffer2[8];
		public int GoldenAgeEndTurn; // -1 if GA not yet triggered

		private fixed byte Flags[4];
		// Are these correct?
		public bool HasVictoriousArmy { get => Util.GetFlag(Flags[0], 0); }
		public bool HasRespawned { get => Util.GetFlag(Flags[0], 1); }

		private int Gold1;
		private int Gold2;
		public int Gold { get => Gold1 + Gold2; }

		// A lot of sections that assume LEAD count is always 32
		// I think this will always be the case for Conquests savs (even scenarios), but things will break for Vanilla (TODO!)
		public float Score;
		private fixed byte UnknownBuffer3[76];
		public int AnarchyTurnsLeft;
		public int Government;
		public int Mobilization;
		private fixed byte UnknownBuffer4[76];
		public int Era;
		public int Beakers;
		public int Researching;
		public int TurnsResearched;
		public int FutureTechsKnown;
		public fixed short UnitsPerStratOwned[32]; // ???
		public fixed short UnitsPerStratInProd[32]; // ???
		public int NumberOfArmies;
		public int NumberOfUnits;
		private fixed byte UnknownBuffer5[4];
		public int NumberOfCities;
		public int NumberOfColonies;
		public int NumberOfContinents;
		private fixed byte UnknownBuffer6[4];
		public int LuxuryRate;
		public int ScienceRate;
		public int TaxRate;

		// Length gap: there are 32 LEAD_LEAD structs here in Sav files

		private fixed byte UnknownBuffer7[128];
		public fixed int RefuseContactForTurns[32];
		private fixed byte UnknownBuffer8[128];
		private fixed int WarWearinessPoints[32];
		private fixed bool WarStatus[32];
		private fixed bool Embassies[32];
		private fixed bool Spies[32];
		private fixed bool FailedSpyMission[32];
		private fixed int BorderViolation[32];
		private fixed int GoldPerTurnTo[32];
		private fixed int Contact[32];
		private fixed int Agreements[32];
		private fixed int Alliances[32];
		private fixed int Embargoes[32];
		private fixed byte UnknownBuffer9[72];
		public int Color;

		private fixed byte Text[176];
		public string LeaderName { get => Util.GetString(ref this, 1896, 32); }
		public string Title { get => Util.GetString(ref this, 1928, 24); }
		public string Name { get => Util.GetString(ref this, 1952, 40); }
		public string Noun { get => Util.GetString(ref this, 1992, 40); }
		public string Adjective { get => Util.GetString(ref this, 2032, 40); }

		public bool Gender; // 0: Male, 1: Female
		private fixed byte Padding[3];
		private fixed byte UnknownBuffer10[24];

		// if Save Version >= 18.00
		private fixed byte UnknownBuffer11[4];
		public int VictoryPoints;
		private fixed byte UnknownBuffer12[4];
		public int VictoryPointsFromLocation;
		public int VictoryPointsFromCapture;
		private fixed byte UnknownBuffer13[40];

		// if Save Version >= 18.06
		private fixed byte Text2[228];
		private fixed byte UnknownBuffer14[32];
		public int FoundedCities;

		private fixed byte UnknownBuffer15[684];

		// Length gap: there are several dynamic length arrays here based on BLDG, PRTO, Spaceship parts, GOOD, and CONT

		public CULT Cult;
		public ESPN Espn1;
		public ESPN Espn2;
		public int ScienceQueueSize;

		// Length gap: one 4-byte integer for each of the science queue

		private fixed byte UnknownBuffer16[292];
	}
}
