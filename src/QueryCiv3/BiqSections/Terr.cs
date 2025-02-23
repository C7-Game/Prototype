using System;
using System.Runtime.InteropServices;

namespace QueryCiv3.Biq {
	[StructLayout(LayoutKind.Sequential, Pack = 1)]
	public unsafe struct TERR {
		public int Length;
		public int NumPossibleResources;
		/*
            In the TERR section of Civ 3 BIQ files, there is a bit array for possible resources
            One bit for each resource (as defined in GOOD), round up to the nearest byte for space
            Because this is a dynamic-length section, it is stored separately from this struct
        */

		private fixed byte Text[64];
		public string Name { get => Util.GetString(ref this, 8, 32); }
		public string CivilopediaEntry { get => Util.GetString(ref this, 40, 32); }

		public int IrrigationBonus;
		public int MiningBonus;
		public int RoadBonus;
		public int DefenseBonus;
		public int MovementCost;
		public int Food;
		public int Shields;
		public int Commerce;
		// Which worker job (TFRM) can be performed on this terrain type
		public int WorkerJobAllowed;
		// Which Terrain this Terrain becomes if affected by pollution.  -1 = not affected.  14 = Base Terrain Type (probably 12 in Vanilla/PTW)
		public int PollutionEffect;
		public byte AllowCities;
		public byte AllowColonies;
		public byte Impassable;
		public byte ImpassableByWheeled;
		public byte AllowAirfields;
		public byte AllowForts;
		public byte AllowOutposts;
		public byte AllowRadarTowers;
		private fixed byte Unknown[4];
		public byte LandmarkEnabled;
		public int LandmarkFood;
		public int LandmarkShields;
		public int LandmarkCommerce;
		public int LandmarkIrrigationBonus;
		public int LandmarkMiningBonus;
		public int LandmarkRoadBonus;
		public int LandmarkMovementBonus;
		public int LandmarkDefensiveBonus;

		private fixed byte Text2[64];
		public string LandmarkName { get => Util.GetString(ref this, 157, 32); }
		public string LandmarkCivilopediaEntry { get => Util.GetString(ref this, 189, 32); }

		private fixed byte Unknown2[4];
		public int TerrainFlags;
		public int DiseaseStrength;
	}
}
