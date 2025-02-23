using System;
using System.Runtime.InteropServices;

namespace QueryCiv3.Sav {
	[StructLayout(LayoutKind.Sequential, Pack = 1)]
	public unsafe struct PLGI {
		private fixed byte HeaderText[4];
		public int Length;
		private fixed byte UnknownBuffer[4];
		private fixed byte HeaderText2[4];
		public int Length2;
		private fixed byte UnknownBuffer2[8];
	}
}
