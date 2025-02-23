using System;
using System.Runtime.InteropServices;

namespace QueryCiv3.Biq {
	public struct GOVT_GOVT {
		public int CanBribe;
		public int BriberyModifier;
		public int ResistanceModifier;
	}

	[StructLayout(LayoutKind.Sequential, Pack = 1)]
	public unsafe struct GOVT {
		public int Length;
		public int DefaultType;
		public int TransitionType;
		public int RequiresMaintenance;
		public int Toggle1; // ??? 0: Republic/Democracy, 1: Other
		public int TilePenalty;
		public int TradeBonus;

		private fixed byte Text[352];
		public string Name { get => Util.GetString(ref this, 28, 64); }
		public string CivilopediaEntry { get => Util.GetString(ref this, 92, 32); }
		public string MaleRulerTitle1 { get => Util.GetString(ref this, 124, 32); }
		public string FemaleRulerTitle1 { get => Util.GetString(ref this, 156, 32); }
		public string MaleRulerTitle2 { get => Util.GetString(ref this, 188, 32); }
		public string FemaleRulerTitle2 { get => Util.GetString(ref this, 220, 32); }
		public string MaleRulerTitle3 { get => Util.GetString(ref this, 252, 32); }
		public string FemaleRulerTitle3 { get => Util.GetString(ref this, 284, 32); }
		public string MaleRulerTitle4 { get => Util.GetString(ref this, 316, 32); }
		public string FemaleRulerTitle4 { get => Util.GetString(ref this, 348, 32); }


		public int Corruption;
		public int ImmuneTo;
		public int DiplomatsAre;
		public int SpiesAre;
		public int NumberOfGovernments;

		/*
            Dynamic length gap
            In BIQ files, there are 3 integers here for each government in NumberOfGovernments
            Data is instead stored in 2d array of GOVTGOVT
        */

		public int Hurrying;
		public int AssimilationChance;
		public int DraftLimit;
		public int MilitaryPoliceLimit;
		public int RulerTitlePairsUsed;
		public int PrerequisiteTechnology;
		public int ScienceRateCap;
		public int WorkerRate;
		public int Toggle2; // ??? -1: Despotism/Communism, 0: Anarchy/Monarchy, 1: Republic/Democracy
		public int Toggle3; // ??? 0: Other, 1: Republic/Democracy
		private fixed byte UnknownBuffer[4];
		public int FreeUnits;
		public int FreeUnitsPerTown;
		public int FreeUnitsPerCity;
		public int FreeUnitsPerMetropolis;
		public int UnitCost;
		public int WarWeariness;
		public int Xenophobic;
		public int ForceResettle;
	}
}
