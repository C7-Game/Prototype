using System;
using System.Runtime.InteropServices;

namespace QueryCiv3.Sav {
	[StructLayout(LayoutKind.Sequential, Pack = 1)]
	public unsafe struct GAME {
		private fixed byte HeaderText1[4];
		public int Length;
		private fixed byte UnknownBuffer[4];

		// TODO: fully populate flags:
		private fixed byte Flags[12];
		public bool DominationVictory { get => Util.GetFlag(Flags[4], 0); }
		public bool SpaceRaceVictory { get => Util.GetFlag(Flags[4], 1); }
		public bool DiplomaticVictory { get => Util.GetFlag(Flags[4], 2); }
		public bool ConquestVictory { get => Util.GetFlag(Flags[4], 3); }
		public bool CulturalVictory { get => Util.GetFlag(Flags[4], 4); }
		public bool CivSpecificAbilities { get => Util.GetFlag(Flags[4], 5); }
		public bool CulturallyLinkedStart { get => Util.GetFlag(Flags[4], 6); }
		public bool RestartPlayers { get => Util.GetFlag(Flags[4], 7); }

		public bool PreserveRandomSeed { get => Util.GetFlag(Flags[5], 0); }
		public bool AcceleratedProduction { get => Util.GetFlag(Flags[5], 1); }
		public bool Elimination { get => Util.GetFlag(Flags[5], 2); }
		public bool Regicide { get => Util.GetFlag(Flags[5], 3); }
		public bool MassRegicide { get => Util.GetFlag(Flags[5], 4); }
		public bool VictoryLocations { get => Util.GetFlag(Flags[5], 5); }
		public bool CaptureTheFlag { get => Util.GetFlag(Flags[5], 6); }
		public bool AllowCulturalConversions { get => Util.GetFlag(Flags[5], 7); }


		public int DifficultyID;
		private fixed byte UnknownBuffer2[4];
		public int NumberOfUnits;
		public int NumberOfCities;
		public int NumberOfColonies;
		public int NukesUsed;
		public int GlobalWarmingState; // 0: No sun, 1: Yellow, 2: Yellow with red border, 3: Red
		public int VictoryType; // -1 if game still ongoing
		public int Winner; // LEAD id
		private fixed byte UnknownBuffer3[4];
		public int TurnNumber;
		public int GameYear;
		public int RandomSeed;
		private fixed byte UnknownBuffer4[4];
		public IntBitmap HumanPlayers;
		public IntBitmap RemainingPlayers;
		public IntBitmap RemainingRaces;
		private fixed byte UnknownBuffer5[24];
		public byte PowerbarCheck;
		public byte MegaTrainerXLCheck;
		private fixed byte UnknownBuffer6[54];
		private fixed int UnknownBuffer7[32];
		public int NumberOfContinents;
		public int NumberOfPlayers;

		// The following sections are conditional based on the type of SAV file
		// For full backwards compatibility, these would need to be broken out into their own structs
		// However, for now, for simplificy, we'll assume that we're only dealing with Conquests files

		// if Save Version >= 18.00
		public int NumberOfAirbases;
		public int NumberOfVPLocations;
		public int NumberOfRadarTowers;
		public int NumberOfOutposts;
		public int NextPlayerID;
		private fixed byte UnknownBuffer8[1];

		// if Save Version >= 18.06
		private fixed byte Text[228];
		public string AdminPassword { get => Util.GetString(ref this, 329, 228); }

		private fixed byte UnknownBuffer9[39];
		public int VPLimit;
		public int TurnLimit;
		public int TimePlayed;

		// if Save Version >= 24.08
		private fixed byte UnknownBuffer10[204];
		public int CityEliminationCount;
		public int OneCityCultureWin;
		public int AllCitiesCultureWin;
		public int DominationTerrain;
		public int DominationPopulation;
		public int WonderCost;
		public int DefeatingOpposingUnitCost;
		public int AdvancementCost;
		public int CityConquestPopulation;
		public int VictoryPointScoring;
		public int CapturingSpecialUnit;

	}
}
