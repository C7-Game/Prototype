using System;
using System.Runtime.InteropServices;

namespace QueryCiv3.Sav
{
    [StructLayout(LayoutKind.Sequential, Pack=1)]
    public unsafe struct TILE
    {
        private fixed byte HeaderText1[4];
        public int Length1;
        public byte RiverInfo;
        public bool RiverNortheast { get => Util.GetFlag(RiverInfo, 1); }
        public bool RiverSoutheast { get => Util.GetFlag(RiverInfo, 3); }
        public bool RiverSouthwest { get => Util.GetFlag(RiverInfo, 5); }
        public bool RiverNorthwest { get => Util.GetFlag(RiverInfo, 7); }
        public byte Owner;
        private fixed byte UnknownBuffer[2];
        public int ResourceID;
        public int TopUnitID;
        public byte TextureLocation; // Between 0 and 80 inclusive (9*9 grid)
        public byte TextureFile; // 0: xtgc, 1: xgpc, 2: xdgc, 3: xdpc, 4: xdgp, 5: xggc, 6: wcso, 7: wsss, 8: wooo
        private fixed byte Flags1[6];
        public short BarbarianCamp;
        public short CityID;
        public short ColonyID;
        public short Continent;
        private fixed byte UnknownBuffer3[4];
        public bool HasRuins;
        private fixed byte PaddingBuffer[3];

        // if civ 3 conquests file (for now we'll assume this is always true)
        private fixed byte HeaderText2[4];
        public int Length2;

        // TODO: Most flags need to be added
        private fixed byte Flags2[12];
        public int BaseTerrain { get => Flags2[5] & 0x0f; }
        public int OverlayTerrain { get => (Flags2[5] & 0xf0) >> 4; }

        public bool BonusShield { get => Util.GetFlag(Flags2[10], 0); }
        public bool SnowCapped { get => Util.GetFlag(Flags2[10], 4); }
        public bool PineForest { get => Util.GetFlag(Flags2[10], 5); }

        private fixed byte HeaderText3[4];
        public int Length3;
        private fixed byte UnknownBuffer4[4];
        // end if

        private fixed byte HeaderText4[4];
        public int Length4;
        public IntBitmap ExploredBy;
        public IntBitmap VisibleTo; // Visible right now to civ by units
        public IntBitmap VisibleTo2; // Visible right now to civ by ???
        public IntBitmap VisibleTo3; // Visible right now to civ by city
        private fixed byte UnknownBuffer5[4];
        public short CityIDOfCitizen;
        public fixed short TradeRoutes[32];
        public fixed byte BonusBits[32];
        private fixed byte UnknownBuffer6[10];
    }
}
