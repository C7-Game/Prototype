using System;
using System.Collections.Generic;

namespace QueryCiv3
{
    public class SavData
    {
        public BicData Bic;
        public BicData MediaBic;
        public Civ3File Sav;
        public byte[] DefaultBic;
        public GameSection Game;
        public WrldSection Wrld;
        public bool HasCustomBic
        {
            get => (uint)Sav.ReadInt32(12 + Sav.SectionOffset("VER#", 1)) != (uint)0xcdcdcdcd;
        }
        public byte[] CustomBic
        { get {
            if(HasCustomBic)
            {
                int Start;
                int End;
                try { Start = Sav.SectionOffset("BICX", 1); }
                catch
                {
                    try { Start = Sav.SectionOffset("BICQ", 1); }
                    catch { Start = Sav.SectionOffset("BIC ", 1); }
                }
                // Offset doesn't include section header bytes
                Start -= 4;
                try {
                    End = Sav.SectionOffset("GAME", 2) - 4;
                }
                catch { End = Sav.FileData.Length; }
                List<byte> CustomBic = new List<byte>();
                for(int i=Start; i<End; i++) { CustomBic.Add(Sav.FileData[i]); }
                return CustomBic.ToArray();
            }
            return null;
        }}
        public SavData(byte[] savBytes, byte[] defaultBic)
        {
            // TODO: Should I just take a BicData instead of byte array?
            DefaultBic = defaultBic;
            Sav = new Civ3File(savBytes);
            Init();
        }
        protected void Init()
        {
            if(HasCustomBic)
            {
                MediaBic = new BicData(CustomBic);
                if(MediaBic.HasCustomRules)
                    Bic = MediaBic;
                else
                    Bic = new BicData(DefaultBic);
            }
            else
            {
                Bic = new BicData(DefaultBic);
            }
            Wrld = new WrldSection(this);
            Game = new GameSection(this);
        }
        public MapTile[] Tile
        { get {
            int TileOffset = Sav.SectionOffset("TILE", 1);
            int TileLength = 212;
            int TileCount = Wrld.Height * (Wrld.Width / 2);
            List<MapTile> TileList = new List<MapTile>();
            for(int i=0; i< TileCount; i++, TileOffset += TileLength)
            {
                int y = i / (Wrld.Width / 2);
                TileList.Add(new MapTile(this, TileOffset, (i % (Wrld.Width / 2)) * 2 + (y % 2), y));
            }
            return TileList.ToArray();
        }}
        // TODO: Use ListSection for this?
        public ContItem[] Cont
        { get {
            int ContCount = Wrld.ContinentCount;
            List<ContItem> LeadList = new List<ContItem>();
            for(int i=0; i< ContCount; i++)
            {
                int LeadOffset = Sav.SectionOffset("CONT", i+1);
                LeadList.Add(new ContItem(this, LeadOffset));
            }
            return LeadList.ToArray();
        }}
        public LeaderItem[] Lead
        { get {
            int LeadCount = 32;
            List<LeaderItem> LeadList = new List<LeaderItem>();
            for(int i=0; i< LeadCount; i++)
            {
                int LeadOffset = Sav.SectionOffset("LEAD", i+1);
                LeadList.Add(new LeaderItem(this, LeadOffset));
            }
            return LeadList.ToArray();
        }}
        public CityItem[] City
        { get {
            // Since "CITY" can sometimes appear in dirty data, let's find the first
            //   "CITY" with 0x00000088 after it
            int CityOffset = 0;
            for(int i=0, chk=0; chk!=0x88; i++)
            {
                CityOffset = Sav.SectionOffset("CITY", i+1);
                chk = Sav.ReadInt32(CityOffset);
            }
            int CityCount = 1; // TEMP HACK // Game.CityCount;

            CityItem[] CityList = new CityItem[CityCount];
            for(int i=0; i< CityCount; i++)
            {
                CityList[i] = new CityItem(this, CityOffset);
                // incomplete; still need to account for bytes after Ctzn and list w/count after buildings stats
                CityOffset += 0x228 + (CityList[i].CitizenCount * 300) + (Bic.Bldg.Length * 8) + 0x30;
            }
            return CityList;
        }}
    }
}