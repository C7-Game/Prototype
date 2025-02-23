using System;
using System.Runtime.InteropServices;

namespace QueryCiv3.Biq {
	[StructLayout(LayoutKind.Sequential, Pack = 1)]
	public unsafe struct EXPR {
		public int Length;

		private fixed byte Text[32];
		public string Name { get => Util.GetString(ref this, 4, 32); }

		public int BaseHitPoints;
		public int RetreatBonus;
	}
}
