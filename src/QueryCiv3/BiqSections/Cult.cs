using System;
using System.Runtime.InteropServices;

namespace QueryCiv3.Biq {
	[StructLayout(LayoutKind.Sequential, Pack = 1)]
	public unsafe struct CULT {
		public int Length;

		private fixed byte Text[64];
		public string Name { get => Util.GetString(ref this, 4, 64); }

		public int ChanceOfSuccessfulPropaganda;
		public int CultureRatioPercentage;
		public int CultureRatioDenominator;
		public int CultureRatioNumerator;
		public int InitialResistanceChance;
		public int ContinuedResistanceChance;
	}
}
