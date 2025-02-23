using System;
using System.Runtime.InteropServices;

namespace QueryCiv3.Sav {
	[StructLayout(LayoutKind.Sequential, Pack = 1)]
	public unsafe struct DATE {
		private fixed byte HeaderText[4];
		public int Length;

		private fixed byte Text[64];
		public string YearText { get => Util.GetString(ref this, 8, 64); }

		public int BaseUnit;
		public int Month;
		public int Week;
		public int Year;
		private fixed byte UnknownBuffer[4];
	}
}
