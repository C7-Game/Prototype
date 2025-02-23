using System;
using System.Runtime.InteropServices;

namespace QueryCiv3.Sav {
	[StructLayout(LayoutKind.Sequential, Pack = 1)]
	public unsafe struct Turn {
		public int TurnNumber;
		public int Date;
		public int RemainingCivs;
	}

	[StructLayout(LayoutKind.Sequential, Pack = 1)]
	public unsafe struct HIST {
		private fixed byte HeaderText[4];
		public int TurnCount;
		public IntBitmap Civs;
	}
}
