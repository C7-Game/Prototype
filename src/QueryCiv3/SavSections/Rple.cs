using System;
using System.Runtime.InteropServices;

namespace QueryCiv3.Sav {
	[StructLayout(LayoutKind.Sequential, Pack = 1)]
	public unsafe struct RPLE {
		private fixed byte HeaderText[4];
		public int DataHeader;
		public byte EventType;
		private fixed byte UnknownBuffer1[1];
		public short XLocation;
		public short YLocation;
		private fixed byte UnknownBuffer2[4];

		// RPLE ends in a null-terminated string, which is variable length, so it can't be included here
	}
}
