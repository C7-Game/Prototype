using System;
using System.Runtime.InteropServices;

namespace QueryCiv3.Sav {
	[StructLayout(LayoutKind.Sequential, Pack = 1)]
	public unsafe struct IDLS {
		private fixed byte HeaderText[4];
		public int Length;
		private fixed byte UnknownBuffer[48];
	}
}
