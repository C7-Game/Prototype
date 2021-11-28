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
     public class GameSection
    {
        private int Offset;
        private SavData Data;
        public GameSection(SavData sav)
        // Need to know continent count for some offsets, not sure if it's somewhere in Game so grabbing it from Wrld
        {
            Data = sav;
            // TODO: Change nth to 1 after separating BIC and SAV data
            Offset = Data.Sav.SectionOffset("GAME", 2);
        }
        // TODO: Return Bic sections, not IDs
        public int DifficultyID { get => Data.Sav.ReadInt32(Offset+20); }
        public DiffSection Difficulty { get => Data.Bic.Diff[DifficultyID]; }
        public int UnitCount { get => Data.Sav.ReadInt32(Offset+28); }
        public int CityCount { get => Data.Sav.ReadInt32(Offset+32); }
        // The per-civ tech list is actually a per-tech 32-bit bitmask, and the number of continents impacts its offset
        public int[] TechCivMask
        { get {
            int MaskOffset = Offset + 856 + (Data.Wrld.ContinentCount * 4);
            int NumTechs = Data.Bic.Tech.Length;
            List<int> Out = new List<int>();
            for(int i=0; i<NumTechs; i++) Out.Add(Data.Sav.ReadInt32(MaskOffset + 4 * i));
            return Out.ToArray();
        }}
    }
   public class WrldSection
    {
        private SavData Data;
        private int Offset1, Offset2, Offset3;
        public WrldSection(SavData sav)
        {
            Data = sav;
            Offset1 = Data.Sav.SectionOffset("WRLD", 1);
            Offset2 = Data.Sav.SectionOffset("WRLD", 2);
            Offset3 = Data.Sav.SectionOffset("WRLD", 3);
        }
        public short ContinentCount { get => Data.Sav.ReadInt16(Offset1+4); }
        public int WsizID { get => Data.Sav.ReadInt32(Offset1+234); }
        public WsizSection WorldSize { get => Data.Bic.Wsiz[WsizID]; }
        public int Height { get => Data.Sav.ReadInt32(Offset2+8); }
        public int Width { get => Data.Sav.ReadInt32(Offset2+28); }
    }
    public class MapTile
    {
        protected SavData Data;
        public int Offset {get; protected set;}
        public int X {get; protected set;}
        public int Y {get; protected set;}
        public MapTile(SavData data, int offset, int x, int y)
        {
            Data = data;
            Offset = offset;
            X = x;
            Y = y;
        }
        public int ContID { get => Data.Sav.ReadByte(Offset+30); }
        // TODO: 0x80 is barbs...sure there's more to this than that
        public int BarbMask { get => Data.Sav.ReadByte(Offset+48); }
        public int Terrain { get => Data.Sav.ReadByte(Offset+53); }
        public int BaseTerrain { get => Terrain & 0x0f; }
        public int OverlayTerrain { get => (Terrain & 0xf0) >> 4; }
        public int BaseTerrainFileID { get => Data.Sav.ReadByte(Offset+17); }
        public int BaseTerrainImageID { get => Data.Sav.ReadByte(Offset+16); }
    }
    public class ContItem
    {
        protected SavData Data;
        protected int Offset;
        public ContItem(SavData data, int offset)
        {
            Data = data;
            Offset = offset;
        }
        public bool IsLand { get => Data.Sav.ReadInt32(Offset+4) != 0; }
        public int TileCount { get => Data.Sav.ReadInt32(Offset+8); }
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
        public RaceSection Race { get => Data.Bic.Race[RaceID]; }
        public int Gold { get => Data.Sav.ReadInt32(Offset+44) + Data.Sav.ReadInt32(Offset+48); }
        public int GovtID { get => Data.Sav.ReadInt32(Offset+136); }
        public GovtSection Govt { get => Data.Bic.Govt[GovtID]; }
        public int MobilizationLevel { get => Data.Sav.ReadInt32(Offset+140); }
        public int TilesDiscovered { get => Data.Sav.ReadInt32(Offset+144); }
        public int ErasID { get => Data.Sav.ReadInt32(Offset+220); }
        public int ResearchBeakers { get => Data.Sav.ReadInt32(Offset+224); }
        public int CurrentResearchTechID { get => Data.Sav.ReadInt32(Offset+228); }
        public TechSection Tech { get => Data.Bic.Tech[CurrentResearchTechID]; }
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
        public RaceSection Race { get => Data.Bic.Race[RaceID]; }
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
