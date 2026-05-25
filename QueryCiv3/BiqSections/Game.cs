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
											 // By playable here, it means these civs' data are included in the game data,
											 // not necessarily that the human player can pick them,
											 // or that they are even a part of the game
		public int NumberOfPlayableCivs; // 0: all (31) playable

		/*
            Dynamic length gap
            In BIQ files, if the NumberOfPlayableCivs is > 0,
            for each playable civ, there is a 1 int long ID for that civ.
            If the NumberOfPlayableCivs is 0 this section has been observed to have 0 length. 
            Data is instead stored in 2d array GameCiv
        */

		private fixed byte Flags[4];
		public bool DominationVictory { get => Util.GetFlag(Flags[0], 0); }
		public bool SpaceRaceVictory { get => Util.GetFlag(Flags[0], 1); }
		public bool DiplomaticVictory { get => Util.GetFlag(Flags[0], 2); }
		public bool ConquestVictory { get => Util.GetFlag(Flags[0], 3); }
		public bool CulturalVictory { get => Util.GetFlag(Flags[0], 4); }
		public bool CivSpecificAbilities { get => Util.GetFlag(Flags[0], 5); } // PTW, or at least not present in Conquests biq >= 12.8
		public bool CulturallyLinkedStart { get => Util.GetFlag(Flags[0], 6); }
		public bool RespawnAiPlayers { get => Util.GetFlag(Flags[0], 7); }

		public bool PreserveRandomSeed { get => Util.GetFlag(Flags[1], 0); }
		public bool AcceleratedProduction { get => Util.GetFlag(Flags[1], 1); }
		public bool CityElimination { get => Util.GetFlag(Flags[1], 2); }
		public bool Regicide { get => Util.GetFlag(Flags[1], 3); }
		public bool MassRegicide { get => Util.GetFlag(Flags[1], 4); }
		public bool VictoryLocations { get => Util.GetFlag(Flags[1], 5); } // 'Victory Point Scoring' flag in Editor
		public bool CaptureTheFlag { get => Util.GetFlag(Flags[1], 6); } // 'Capture the Unit' flag in Editor
		public bool AllowCulturalConversions { get => Util.GetFlag(Flags[1], 7); }

		public bool WonderVictory { get => Util.GetFlag(Flags[2], 0); }
		public bool ReverseCaptureTheFlag { get => Util.GetFlag(Flags[2], 1); }
		public bool AllowScientificLeaders { get => Util.GetFlag(Flags[2], 2); }

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
            In BIQ files, if the NumberOfPlayableCivs is > 0,
            for each playable civ, there is a single int for alliance status.
            If the NumberOfPlayableCivs is 0 this section has been observed to have 0 length. 
            Data is instead stored in 2d array GameAlliance
        */

		// (4 bytes - long		map visible, BIX >= 11.19 ONLY, not BIQ (major=12))

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
		private fixed byte UnknownBuffer2[264]; // one 4-byte int + 260 bytes of string?
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
