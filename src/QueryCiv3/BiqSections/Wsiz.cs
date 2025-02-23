using System;
using System.Runtime.InteropServices;

namespace QueryCiv3.Biq {
	[StructLayout(LayoutKind.Sequential, Pack = 1)]
	public unsafe struct WSIZ {
		public int Length;
		public int OptimalNumberOfCities;
		public int TechRate;

		private fixed byte Text[56];
		public string Name { get => Util.GetString(ref this, 36, 32); }

		public int Height;
		public int DistanceBetweenCivs;
		public int NumberOfCivs;
		public int Width;
	}
}
