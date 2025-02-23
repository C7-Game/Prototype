using System;
using System.Runtime.InteropServices;

namespace QueryCiv3.Sav {
	[StructLayout(LayoutKind.Sequential, Pack = 1)]
	public unsafe struct BINF {
		private fixed byte HeaderText[4];
		public int Length;
		public int BuildingCount;
	}
}
