using System;
using System.Runtime.InteropServices;

namespace QueryCiv3.Sav {
	[StructLayout(LayoutKind.Sequential, Pack = 1)]
	public unsafe struct RADT {
		// It seems that RADT, VLOC, and OUTP all have completely identical structs
		private fixed byte HeaderText[4];
		public int Length;
		public int UniqueID;
		public int XCoordinate;
		public int YCoordinate;
		public int PlayerID; // Index into LEAD
	}
}
