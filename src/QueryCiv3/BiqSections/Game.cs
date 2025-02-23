using System;
using System.Runtime.InteropServices;

namespace QueryCiv3.Biq {
	[StructLayout(LayoutKind.Sequential, Pack = 1)]
	public unsafe struct TIMESCALE {
		private fixed int Values[7];
		public int this[int index] { get => Values[index]; }
	}

	[StructLayout(LayoutKind.Sequential, Pack = 1)]
	public unsafe struct ALLIANCE {
		private fixed byte Text[1280];
		public string this[int i] { get => Util.GetString(ref this, i * 256, 256); }
	}

	[StructLayout(LayoutKind.Sequential, Pack = 1)]
	public unsafe struct ALLIANCEWARS {
		private fixed int Values[25];
		public int this[int i, int j] { get => Values[i * 5 + j]; }
	}

	[StructLayout(LayoutKind.Sequential, Pack = 1)]
	public unsafe struct GAME {
		public int Length;
		public int DefaultGameRules; // 0: don't use, 1: use
		public int DefaultVictoryConditions; // 0: don't use, 1: use
		public int NumberOfPlayableCivs; // 0: all playable

		/*
            Dynamic length gap
            In BIQ files, for each playable civ, there is a 1 int long ID for that civ
            Data is instead stored in 2d array GameCiv
        */

		private fixed byte Flags[4];
		public bool DominationVictory { get => Util.GetFlag(Flags[0], 0); }
		public bool SpaceRaceVictory { get => Util.GetFlag(Flags[0], 1); }
		public bool DiplomaticVictory { get => Util.GetFlag(Flags[0], 2); }
		public bool ConquestVictory { get => Util.GetFlag(Flags[0], 3); }
		public bool CulturalVictory { get => Util.GetFlag(Flags[0], 4); }
		public bool CivSpecificAbilities { get => Util.GetFlag(Flags[0], 5); }
		public bool CulturallyLinkedStart { get => Util.GetFlag(Flags[0], 6); }
		public bool RestartPlayers { get => Util.GetFlag(Flags[0], 7); }

		public bool PreserveRandomSeed { get => Util.GetFlag(Flags[1], 0); }
		public bool AcceleratedProduction { get => Util.GetFlag(Flags[1], 1); }
		public bool Elimination { get => Util.GetFlag(Flags[1], 2); }
		public bool Regicide { get => Util.GetFlag(Flags[1], 3); }
		public bool MassRegicide { get => Util.GetFlag(Flags[1], 4); }
		public bool VictoryLocations { get => Util.GetFlag(Flags[1], 5); }
		public bool CaptureTheFlag { get => Util.GetFlag(Flags[1], 6); }
		public bool AllowCulturalConversions { get => Util.GetFlag(Flags[1], 7); }

		public int PlaceCaptureUnits;
		public int AutoPlaceKings;
		public int AutoPlaceVictoryLocations;
		public int DebugMode;
		public int UseTimeLimit;
		public int BaseTimeUnit; // 0: Years, 1: Months, 2: Weeks
		public int StartMonth;
		public int StartWeek;
		public int StartYear;
		public int MinuteTimeLimit;
		public int TurnTimeLimit;
		public TIMESCALE TimescaleNumberOfTurns;
		public TIMESCALE TurnNumberOfTimeUnits;

		private fixed byte Text[5200];
		public string ScenarioSearchFolders { get => Util.GetString(ref this, 120, 5200); }

		/*
            Dynamic length gap
            In BIQ files, for each playable civ, there is a single int for alliance status
            Data is instead stored in 2d array GameAlliance
        */

		public int VictoryPointLimit;
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
		private fixed byte UnknownBuffer[5];
		public ALLIANCE AllianceNames;
		public ALLIANCEWARS WarWithAlliance;
		public int AllianceVictoryType;

		private fixed byte Text3[260];
		public string PlagueName { get => Util.GetString(ref this, 6757, 260); }

		public byte PermitPlagues;
		public int PlagueEarliestStart;
		public int PlagueVartiation;
		public int PlagueDuration;
		public int PlagueStrength;
		public int PlagueGracePeriod;
		public int PlagueMaxOccurance;
		private fixed byte UnknownBuffer2[264];
		public int RespawnFlagUnits;
		public byte CaptureAnyFlag;
		public int GoldForCapture;
		public byte MapVisible;
		public byte RetainCulture;
		private fixed byte UnknownBuffer3[4];
		public int EruptionPeriod;
		public int MPBasetime;
		public int MPCityTime;
		public int MPUnitTime;
	}
}
