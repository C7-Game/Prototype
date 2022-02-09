using System;
using System.Runtime.InteropServices;

namespace QueryCiv3.Sav
{
    [StructLayout(LayoutKind.Sequential, Pack=1)]
    public unsafe struct CITY_Building
    {
        public int Year;
        public int BuiltByPlayer;
        public int Culture;
    }

    [StructLayout(LayoutKind.Sequential, Pack=1)]
    public unsafe struct CITY
    {
        private fixed byte HeaderText1[4];
        public int Length;
        public int ID;
        public short X;
        public short Y;
        public byte Owner;
        private fixed byte UnknownBuffer[3];
        public int MaintenanceGPT;

        // TODO: Fully populate flags
        private fixed byte Flags[16];
        public bool CivilDisorder       { get => Util.GetFlag(Flags[0], 0); }
        public bool WeLoveTheKingDay    { get => Util.GetFlag(Flags[0], 1); }
        public bool HasFreshWaterAccess { get => Util.GetFlag(Flags[0], 5); }

        public int TotalFood;
        public int ShieldsCollected;
        public int Pollution;
        public int Constructing; // Index into BLDG or UNIT
        public int ConstructingType; // 0: wealth, 1: building, 2: unit
        public int YearBuilt;
        private fixed byte UnknownBuffer2[4];
        public int Culture;
        public int MilitaryPolice;
        public int LuxuryConnectedCount;
        public IntBitmap LuxuryConnectedBits;
        private fixed byte UnknownBuffer3[4];
        public int DraftTurnsLeft;
        private fixed byte UnknownBuffer4[52];
        private fixed byte HeaderText2[4];
        public int Length2;
        public byte UnhappyNoReasonPercent;
        public byte UnhappyCrowdedPercent;
        public byte UnhappyWarWearinessPercent;
        public byte UnhappyAgresssionPercent;
        public byte UnhappyPropagandaPercent;
        public byte UnhappyDraftPercent;
        public byte UnhappyOppressionPercent;
        public byte UnhappyThisCityImprovementsPercent;
        public byte UnhappyOtherCityImprovementsPercent;
        private fixed byte UnknownBuffer5[7];
        private fixed byte HeaderText3[4];
        public int Length3;
        private fixed byte UnknownBuffer6[36];
        private fixed byte HeaderText4[4];
        public int Length4;
        public int CulturePerTurn;
        public fixed int CulturePerLead[32];
        private fixed byte UnknownBuffer7[8];
        public int FoodPerTurn;
        public int ShieldsPerTurn;
        public int CommercePerTurn;
        private fixed byte UnknownBuffer8[12];
        private fixed byte HeaderText5[4];
        public int Length5;

        private fixed byte Text[24];
        public string Name { get => Util.GetString(ref this, 392, 24); }

        public int QueueSlotsUsed;
        public fixed int Queue[18]; // Indexes 0,2,4... are BLDG or UNIT index, Indexes 1,3,5... are types

        public int FoodPerTurnForPopulation;
        public int CorruptShieldsPerTurn;
        public int CorruptGoldPerTurn;
        public int ExcessFoodPerTurn;
        public int UnwastedFoodPerTurn;
        public int UncorruptGoldPerTurn;
        public int LuxuryGoldPerTurn;
        public int ScienceGoldPerTurn;
        public int TreasuryGoldPerTurn;
        public int EntertainerCount;
        public int ScientistCount;
        public int TaxCollectorCount;

        public POPD Popd;

        // Dynamic length gap: a CTZN for each Popd.CitizenCount

        public BINF Binf;

        // Dynamic length gap: a CITY_Building for each Binf.BuildingCount

        public BITM Bitm;

        // if version >= ptw:
        public DATE Date;
    }
}
