using System;
using System.Runtime.InteropServices;

namespace QueryCiv3.Biq {
	[StructLayout(LayoutKind.Sequential, Pack = 1)]
	public unsafe struct CONT {
		public int Length;
		public int Type; // 0: Water, 1: Land
		public int NumberOfTiles;
	}
}
