using System;
using System.Runtime.InteropServices;

namespace QueryCiv3.Biq {
	[StructLayout(LayoutKind.Sequential, Pack = 1)]
	public unsafe struct TILE {
		// TILE has one of the more complex data structures, and so the list of values and flags available here is not necessarily complete
		// There is also repetition between Vanilla data vs PTW/Conquests data
		// For instance, river data is available in 3 places, though only two seem to be set with values
		// More thorough documentation can be considered a TODO for addressing these peculiarities
		public int Length;

		private byte RiverConnections;
		public bool RiverConnectionNorth { get => Util.GetFlag(RiverConnections, 0); }
		public bool RiverConnectionNortheast { get => Util.GetFlag(RiverConnections, 1); }
		public bool RiverConnectionEast { get => Util.GetFlag(RiverConnections, 2); }
		public bool RiverConnectionSoutheast { get => Util.GetFlag(RiverConnections, 3); }
		public bool RiverConnectionSouth { get => Util.GetFlag(RiverConnections, 4); }
		public bool RiverConnectionSouthwest { get => Util.GetFlag(RiverConnections, 5); }
		public bool RiverConnectionWest { get => Util.GetFlag(RiverConnections, 6); }
		public bool RiverConnectionNorthwest { get => Util.GetFlag(RiverConnections, 7); }


		public byte Border;
		public int Resource;
		public byte TextureLocation; // Between 0 and 80 inclusive (9*9 grid)
		public byte TextureFile; // 0: xtgc, 1: xgpc, 2: xdgc, 3: xdpc, 4: xdgp, 5: xggc, 6: wcso, 7: wsss, 8: wooo
		private fixed byte UnknownBuffer[2];
		public byte OverlayFlags;
		public byte Terrain;
		public byte BonusFlags;
		public byte RiverCrossingFlags; // Appears to always be zero. We can use river connection info to figure out where rivers are instead
		public short BarbarianTribe;
		public short Colony;
		public short City;
		public short Continent;
		private fixed byte UnknownBuffer2[1];
		public short VictoryPointLocation; // 0: VPL, -1: Not a VPL
		public int Ruin;

		private fixed byte C3COverlays[4];
		public bool Road { get => Util.GetFlag(C3COverlays[0], 0); }
		public bool Railroad { get => Util.GetFlag(C3COverlays[0], 1); }
		public bool Mine { get => Util.GetFlag(C3COverlays[0], 2); }
		public bool Irrigation { get => Util.GetFlag(C3COverlays[0], 3); }
		public bool Fortress { get => Util.GetFlag(C3COverlays[0], 4); }
		public bool GoodyHut { get => Util.GetFlag(C3COverlays[0], 5); }
		public bool Pollution { get => Util.GetFlag(C3COverlays[0], 6); }
		public bool BarbarianCamp { get => Util.GetFlag(C3COverlays[0], 7); }
		public bool Crater { get => Util.GetFlag(C3COverlays[1], 0); }
		public bool Barricade { get => Util.GetFlag(C3COverlays[3], 4); }
		public bool Airfield { get => Util.GetFlag(C3COverlays[3], 5); }
		public bool RadarTower { get => Util.GetFlag(C3COverlays[3], 6); }
		public bool Outpost { get => Util.GetFlag(C3COverlays[3], 7); }

		private fixed byte UnknownBuffer3[1];
		private fixed byte C3CTerrain[1];
		public byte BaseTerrain => (byte)((C3CTerrain[0] & 0x0F));
		public byte OverlayTerrain => (byte)((C3CTerrain[0] & 0xF0) >> 4);

		private fixed byte UnknownBuffer4[2];
		public short FogOfWar;

		private fixed byte C3CBonuses[4];
		public bool BonusGrassland { get => Util.GetFlag(C3CBonuses[0], 0); }
		public bool PlayerStart { get => Util.GetFlag(C3CBonuses[0], 3); }
		public bool SnowCappedMountain { get => Util.GetFlag(C3CBonuses[0], 4); }
		public bool PineForest { get => Util.GetFlag(C3CBonuses[0], 5); }
		public bool RiverNorth { get => Util.GetFlag(C3CBonuses[1], 0); }
		public bool RiverWest { get => Util.GetFlag(C3CBonuses[1], 1); }
		public bool RiverEast { get => Util.GetFlag(C3CBonuses[1], 2); }
		public bool RiverSouth { get => Util.GetFlag(C3CBonuses[1], 3); }
		public bool IsLandmark { get => Util.GetFlag(C3CBonuses[3], 1); }

		private fixed byte UnknownBuffer5[2];
	}
}
