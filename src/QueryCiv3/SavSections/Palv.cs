using System;
using System.Runtime.InteropServices;

namespace QueryCiv3.Sav {
	[StructLayout(LayoutKind.Sequential, Pack = 1)]
	public unsafe struct PALV {
		private fixed byte HeaderText[4];
		public int Length;
		private fixed byte UnknownBuffer[4];
		private fixed int SectionCultures[32]; // 0: American, 1: European, 2: Med, 3: Mideast, 4: Asian
		private fixed byte UnknownBuffer2[8];
		public IntBitmap Sections;

		// if version >= ptw:
		public int UnusedUpgrades;
	}
}
