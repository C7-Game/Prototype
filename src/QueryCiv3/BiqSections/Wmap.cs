using System;
using System.Runtime.InteropServices;

namespace QueryCiv3.Biq {
	[StructLayout(LayoutKind.Sequential, Pack = 1)]
	public unsafe struct WMAP {
		public int Length;
		public int NumberOfResources;

		/*
            Dynamic length gap
            In BIQ files, for each resource, 4 bytes are used to store that resource's ID
            Data is instead stored in 2d array WmapResource;
        */

		public int NumberOfContinents;
		public int Height;
		public int DistanceBetweenCivs;
		public int NumberOfCivs;
		private fixed byte UnknownBuffer[8];
		public int Width;
		private fixed byte UnknownBuffer2[128];
		public int MapSeed;

		private fixed byte Flags[4];
		public bool XWrapping { get => Util.GetFlag(Flags[0], 0); }
		public bool YWrapping { get => Util.GetFlag(Flags[0], 1); }
		public bool PolarIceCaps { get => Util.GetFlag(Flags[0], 2); }
	}
}
