using System;
using System.Runtime.InteropServices;

namespace QueryCiv3.Sav {
	[StructLayout(LayoutKind.Sequential, Pack = 1)]
	public unsafe struct CONT {
		private fixed byte HeaderText[4];
		public int Length;
		public bool Type; // 0: land, 1: water
		private fixed byte Padding[3];
		public int TileCount;
	}
}
