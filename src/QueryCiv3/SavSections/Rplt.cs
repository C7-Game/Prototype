using System;
using System.Runtime.InteropServices;

namespace QueryCiv3.Sav {
	[StructLayout(LayoutKind.Sequential, Pack = 1)]
	public unsafe struct RPLT {
		private fixed byte HeaderText[4];
		public int DataHeader;
		public int TurnNumber;
		public bool ReloadIndicator;
		public int EventCount;
	}
}
