using System;
using System.Collections.Generic;

namespace QueryCiv3
{
    /*
    public class GenericSection
    {
        public int Offset { get; protected set; }
        private SavData Data;
        private int Length;
        public GenericSection(SavData data, string headerString, int length = 256)
        {
            Data = data;
            Offset = Data.Sav.SectionOffset(headerString, 1);
            Length = length;
        }
        public byte[] RawBytes { get => Data.Sav.GetBytes(Offset, Length); }
    }

    public class CityItem
    {
        protected SavData Data;
        protected int Offset;
        protected int CtznOffset = 0x230;
        protected int BinfOffset;
        public CityItem(SavData data, int offset)
        {
            Data = data;
            Offset = offset;
            BinfOffset = (CitizenCount * 300) + Offset + 0x228;
        }
        // I had this as "id" but also as the number of BINF sections?
        public int CityID { get => Data.Sav.ReadInt32(Offset+4); }
        public int X { get => Data.Sav.ReadInt16(Offset+8); }
        public int Y { get => Data.Sav.ReadInt16(Offset+10); }
        public int RaceID { get => Data.Sav.ReadInt32(Offset+12); }
        public Biq.RACE Race { get => Data.Bic.Race[RaceID]; }
        public int StoredFood { get => Data.Sav.ReadInt32(Offset+36); }
        public int StoredShields { get => Data.Sav.ReadInt32(Offset+40); }
        public string Name { get => Data.Sav.GetString(Offset+0x184, 20); }
        public int CitizenCount { get => Data.Sav.ReadInt32(Offset+0x224); }
        // 0x228 is CTZN header, then length, then 2 bytes, then string
        public CityCtzn[] Ctzn
        { get {
            // Not sure they're all the same
            int CtznLength = 300;
            CityCtzn[] Out = new CityCtzn[CtznLength];
            for(int i=0; i<CitizenCount; i++) Out[i] = new CityCtzn(Data, Offset+0x22c+(i*CtznLength));
            return Out;
        }}
        public byte[] RawBytes { get => Data.Sav.GetBytes(Offset+0x228, 304); }
    }
    public class CityCtzn
    {
        protected SavData Data;
        protected int Offset;
        public CityCtzn(SavData data, int offset)
        {
            Data = data;
            Offset = offset;
        }
        public byte[] RawBytes { get => Data.Sav.GetBytes(Offset, 64); }
    }
    */
}
