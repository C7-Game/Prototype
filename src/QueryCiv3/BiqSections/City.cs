using System;
using System.Runtime.InteropServices;

namespace QueryCiv3.Biq {
	[StructLayout(LayoutKind.Sequential, Pack = 1)]
	public unsafe struct CITY {
		public int Length;
		public byte HasWalls;
		public byte HasPalace;

		private fixed byte Text[24];
		public string Name { get => Util.GetString(ref this, 6, 30); }

		public int OwnerType; // 0: None, 1: Barb, 2: Civ, 3: Player
		public int NumberOfBuildings;

		/*
            Dynamic length gap
            In BIQ files, for each buildings, 4 bytes of space are used to point to building id
            Data is instead stored in 2d array CityBuilding
        */

		public int Culture;
		public int Owner;
		public int Size;
		public int X;
		public int Y;
		public int CityLevel;
		public int BorderLevel;
		public int UseAutoName;
	}
}
