using System;
using System.Runtime.InteropServices;

namespace QueryCiv3.Sav {
	[StructLayout(LayoutKind.Sequential, Pack = 1)]
	public unsafe struct WRLD {
		private fixed byte HeaderText1[4];
		public int Length1;
		public short ContinentCount;
		private fixed byte HeaderText2[4];
		public int Length2;
		public int NumberOfLandContinents;
		public int Height;
		public int CivDistance;
		public int NumberOfCivs;
		public int PercentWater; // is this int or float?
		private fixed byte UnknownBuffer1[4];
		public int Width;
		private fixed int PlayerData[32];
		public int WorldSeed;

		private fixed byte Flags[4];
		public bool XWrapping { get => Util.GetFlag(Flags[0], 0); }
		public bool YWrapping { get => Util.GetFlag(Flags[0], 1); }
		public bool PolarIceCaps { get => Util.GetFlag(Flags[0], 2); }

		private fixed byte HeaderText3[4];
		public int Length3;
		public int SelectedClimate;
		public int ActualClimate;
		public int SelectedBarbarians;
		public int ActualBarbarians;
		public int SelectedLandform;
		public int ActualLandform;
		public int SelectedOceanCoverage;
		public int ActualOceanCoverage;
		public int SelectedTemperature;
		public int ActualTemperature;
		public int SelectedAge;
		public int ActualAge;
		public int WsizID;
	}
}
