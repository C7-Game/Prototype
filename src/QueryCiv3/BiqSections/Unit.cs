using System;
using System.Runtime.InteropServices;

namespace QueryCiv3.Biq {
	[StructLayout(LayoutKind.Sequential, Pack = 1)]
	public unsafe struct UNIT {
		public int Length;

		// There are 2 sections in Unit that have space for names, but for PTW and Conquests files only the second buffer is used
		// Because the current BIQ file structure layout targets BIX and BIQ files but not BIC, we can ignore this first buffer for
		//   now unless we decide later on to support full backwards compatibility
		private fixed byte Text[32];
		// public string Name { get => Util.GetString(ref this, 4, 32); }

		public int OwnerType; // 0: None, 1: Barbarian, 2: Civ, 3: Player
		public int ExperienceLevel;
		public int Owner;
		public int UnitType;
		public int AIStrategy;
		public int X;
		public int Y;

		private fixed byte Text2[57];
		public string Name { get => Util.GetString(ref this, 64, 57); }

		public int UseCivilizationKing;
	}
}
