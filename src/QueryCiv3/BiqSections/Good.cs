using System;
using System.Runtime.InteropServices;

namespace QueryCiv3.Biq {
	[StructLayout(LayoutKind.Sequential, Pack = 1)]
	public unsafe struct GOOD {
		public int Length;

		private fixed byte Text[56];
		public string Name { get => Util.GetString(ref this, 4, 24); }
		public string CivilopediaEntry { get => Util.GetString(ref this, 28, 32); }

		public int Type;
		public int AppearanceRatio;
		public int DisappearanceProbability;
		public int Icon;
		public int Prerequisite;
		public int FoodBonus;
		public int ShieldsBonus;
		public int CommerceBonus;
	}
}
