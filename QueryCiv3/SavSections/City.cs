using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace QueryCiv3.Sav {
	[StructLayout(LayoutKind.Sequential, Pack = 1)]
	public unsafe struct CITY_Building {
		public int Year;
		public int BuiltByPlayer;
		public int Culture;
	}

	[StructLayout(LayoutKind.Sequential, Pack = 1)]
	public unsafe struct CITY {
		private fixed byte HeaderText1[4];
		public int Length;
		public int ID;
		public short X;
		public short Y;
		public byte Owner;
		private fixed byte UnknownBuffer[3];
		public int MaintenanceGPT;

		// TODO: Fully populate flags
		private fixed byte Flags[16];
		public bool CivilDisorder { get => Util.GetFlag(Flags[0], 0); }
		public bool WeLoveTheKingDay { get => Util.GetFlag(Flags[0], 1); }
		public bool HasFreshWaterAccess { get => Util.GetFlag(Flags[0], 5); }

		public int TotalFood;
		public int ShieldsCollected;
		public int Pollution;
		public int Constructing; // Index into BLDG or UNIT
		public int ConstructingType; // 0: wealth, 1: building, 2: unit
		public int YearBuilt;
		private int UnknownBuffer2;

		// The power of 10 that will result in the next culture expansion.
		// 1 = 10, 2=100, 3=1000, etc.
		public int NextCultureExpansionPower;
		public int MilitaryPolice;
		public int LuxuryConnectedCount;
		public IntBitmap LuxuryConnectedBits;
		public int NumUnitsDraftedThisTurn;
		public int TurnsOfUnhappinessDueToDrafting;
		private fixed int UnknownBuffer4[13];

		public List<int> GetUnknownBuffer4() {
			List<int> result = new();
			for (int i = 0; i < 13; ++i) {
				result.Add(UnknownBuffer4[i]);
			}
			return result;
		}

		private int HeaderText2;
		public int Length2;
		public byte UnhappyNoReasonPercent;
		public byte UnhappyCrowdedPercent;
		public byte UnhappyWarWearinessPercent;
		public byte UnhappyAgresssionPercent;
		public byte UnhappyPropagandaPercent;
		public byte UnhappyDraftPercent;
		public byte UnhappyOppressionPercent;
		public byte UnhappyThisCityImprovementsPercent;
		public byte UnhappyOtherCityImprovementsPercent;
		private byte UnknownByte1;
		private byte UnknownByte2;
		private byte UnknownByte3;
		private int UnknownBuffer5;
		private int HeaderText3;
		public int Length3;
		private fixed int UnknownBuffer6[9];
		public List<int> GetUnknownBuffer6() {
			List<int> result = new();
			for (int i = 0; i < 13; ++i) {
				result.Add(UnknownBuffer6[i]);
			}
			return result;
		}
		private int HeaderText4;
		public int Length4;
		public int CulturePerTurn;
		private fixed int CulturePerLead[32];

		// The amount of culture this city has for each player. This is tracked
		// per leader because recaptured cities regain their previous culture.
		public List<int> GetCulturePerLeader() {
			List<int> result = new();
			for (int i = 0; i < 32; ++i) {
				result.Add(CulturePerLead[i]);
			}
			return result;
		}

		public int UnhappinessDueToPropaganda; // Exact mechanism unknown
		public int TurnsOfUnhappinessDueToPopRushing;

		public int FoodPerTurn;
		public int ShieldsPerTurn;
		public int CommercePerTurn;
		private int UnknownBuffer9;
		private int UnknownBuffer10;
		private int UnknownBuffer11;
		private int HeaderText5;
		public int Length5;

		private fixed byte Text[24];
		public string Name { get => Util.GetString(ref this, 392, 24); }

		public int QueueSlotsUsed;
		public fixed int Queue[18]; // Indexes 0,2,4... are BLDG or UNIT index, Indexes 1,3,5... are types

		public int FoodPerTurnForPopulation;
		public int CorruptShieldsPerTurn;
		public int CorruptGoldPerTurn;
		public int ExcessFoodPerTurn;
		public int UnwastedFoodPerTurn;
		public int UncorruptGoldPerTurn;
		public int LuxuryGoldPerTurn;
		public int ScienceGoldPerTurn;
		public int TreasuryGoldPerTurn;
		public int EntertainerCount;
		public int ScientistCount;
		public int TaxCollectorCount;

		public POPD Popd;

		// Dynamic length gap: a CTZN for each Popd.CitizenCount

		public BINF Binf;

		// Dynamic length gap: a CITY_Building for each Binf.BuildingCount

		public BITM Bitm;

		// if version >= ptw:
		public DATE Date;
	}
}
