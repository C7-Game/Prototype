using System;
using System.Collections.Generic;
using QueryCiv3.Sav;

namespace QueryCiv3
{
    public class SavData
    {
        public BiqData Bic;
        public Civ3File Sav;

        public GameSection Game;

        public WRLD Wrld;

        private const int SECTION_HEADERS_START = 736;

        public SavData(byte[] savBytes, byte[] biqBytes)
        {
            Bic = new BiqData(biqBytes);
            Load(savBytes);
        }

        public unsafe void Load(byte[] savBytes)
        {
            Sav = new Civ3File(savBytes);
            Game = new GameSection(this);

            fixed (byte* bytePtr = savBytes)
            {
                int offset = 0;
                int header;
                int count = 0;

                while (offset < savBytes.Length - 16) {
                    header = Sav.ReadInt32(offset);

                    switch (header) {
                        case 0x444c5257: // WRLD
                            fixed (void* ptr = &Wrld) {
                                Buffer.MemoryCopy(bytePtr + offset, ptr, sizeof(WRLD), sizeof(WRLD));
                            }
                            offset += sizeof(WRLD);
                            break;
                        default:
                            offset++;
                            break;
                    }
                }
            }
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
