using System;
using System.Runtime.InteropServices;

namespace QueryCiv3.Sav {
	[StructLayout(LayoutKind.Sequential, Pack = 1)]
	public unsafe struct BITM {
		private fixed byte HeaderText[4];
		public int Length;
		private fixed byte UsableBuildingBits[32];
		public int BuildingCount;
		public int BuildingBytes;
	}
}
