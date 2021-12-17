using System;
using System.Collections.Generic;

namespace QueryCiv3
{
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

    public class LeaderItem
    // NOTE: If relative offset is wrong, try adjusting up (or down) by 4
    //  At various phases of my project history I've counted from different base points
    //  Currently offsets are from the end of the 4-char section header, usually where length or count is
    {
        protected SavData Data;
        protected int Offset;
        public LeaderItem(SavData data, int offset)
        {
            Data = data;
            Offset = offset;
        }
        public int PlayerNumber { get => Data.Sav.ReadInt32(Offset+4); }
        public int RaceID { get => Data.Sav.ReadInt32(Offset+8); }
        public Biq.RACE Race { get => Data.Bic.Race[RaceID]; }
        public int Gold { get => Data.Sav.ReadInt32(Offset+44) + Data.Sav.ReadInt32(Offset+48); }
        public int GovtID { get => Data.Sav.ReadInt32(Offset+136); }
        public Biq.GOVT Govt { get => Data.Bic.Govt[GovtID]; }
        public int MobilizationLevel { get => Data.Sav.ReadInt32(Offset+140); }
        public int TilesDiscovered { get => Data.Sav.ReadInt32(Offset+144); }
        public int ErasID { get => Data.Sav.ReadInt32(Offset+220); }
        public int ResearchBeakers { get => Data.Sav.ReadInt32(Offset+224); }
        public int CurrentResearchTechID { get => Data.Sav.ReadInt32(Offset+228); }
        public Biq.TECH Tech { get => Data.Bic.Tech[CurrentResearchTechID]; }
        public int CurrentResearchTurns { get => Data.Sav.ReadInt32(Offset+232); }
        public int FutureTechsCount { get => Data.Sav.ReadInt32(Offset+236); }
        public int ArmiesCount { get => Data.Sav.ReadInt32(Offset+368); }
        public int UnitCount { get => Data.Sav.ReadInt32(Offset+372); }
        public int MilitaryUnitCount { get => Data.Sav.ReadInt32(Offset+376); }
        public int CityCount { get => Data.Sav.ReadInt32(Offset+380); }
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
}
