using System;
using System.Runtime.InteropServices;

namespace QueryCiv3.Biq {
	[StructLayout(LayoutKind.Sequential, Pack = 1)]
	public unsafe struct LEAD_Unit {
		public int NumberOfStartUnits;
		public int UnitType;
	}

	[StructLayout(LayoutKind.Sequential, Pack = 1)]
	public unsafe struct LEAD {
		public int Length;
		public int CustomCivData; // 0: don't use, 1: use
		public int HumanPlayer; // 0: no, 1: yes

		private fixed byte Text[32];
		public string Name { get => Util.GetString(ref this, 12, 32); }

		private fixed byte UnknownBuffer[8]; // Are we sure this buffer isn't just a continuation of the text?
		public int NumberOfStartUnitTypes;

		/*
            Dynamic length gap
            In BIQ files, for each starting unit type, there are 2 ints: one for the amount of that unit and one for its ID
            Data is instead stored in 2d array of LEADPRTO
        */

		public int GenderOfLeaderName;
		public int NumberOfStartingTechnologies;

		/*
            Dynamic length gap
            In BIQ files, for each starting tech, there is 1 int for that tech's ID
            Data is instead stored in 2d array LeadTech
        */

		public int Difficulty;
		public int InitialEra;
		public int StartCash; // $$$$$$$$$$$$$$$$$
		public int Government;
		public int Civ; // -3: any, -2: random
		public int Color;
		public int SkipFirstTurn;
		private fixed byte UnknownBuffer2[4];
		public byte StartEmbassies;
	}
}
