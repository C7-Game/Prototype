using System;
using System.Collections.Generic;
using QueryCiv3.Sav;

namespace QueryCiv3
{
    public unsafe class SavData
    {
        public BiqData Bic;
        public Civ3File Sav;

        public byte* scan;

        public GAME Game;
        public WRLD Wrld;
        public TILE[] Tile;

        public int[] CitiesPerContinent;
        public IntBitmap[] KnownTechFlags;
        public int[] GreatWonderCityIDs;
        public bool[] GreatWondersBuilt;

        private const int SECTION_HEADERS_START = 736;

        public SavData(byte[] savBytes, byte[] biqBytes)
        {
            Bic = new BiqData(biqBytes);
            Load(savBytes);
        }

        public unsafe void Copy<T>(ref T data, int length) where T : unmanaged
        {
            fixed (void* destPtr = &data) {
                Buffer.MemoryCopy(scan, destPtr, length, length);
            }
            scan += length;
        }

        public unsafe void AllocateAndCopy<T>(ref T[] data, int length) where T : unmanaged
        {
            data = new T[length];
            int dataLength = length * sizeof(T);

            fixed (void* destPtr = data) {
                Buffer.MemoryCopy(scan, destPtr, dataLength, dataLength);
            }
            scan += dataLength;
        }

        public unsafe void Load(byte[] savBytes)
        {
            Sav = new Civ3File(savBytes);

            fixed (byte* bytePtr = savBytes)
            {
                int* header;
                scan = bytePtr;
                byte* end = bytePtr + savBytes.Length;

                while (scan < end) {
                    header = (int*)scan;

                    switch (*header) {
                        case 0x454d4147: // GAME
                            Copy(ref Game, sizeof(GAME));
                            AllocateAndCopy(ref CitiesPerContinent, Game.NumberOfContinents);
                            AllocateAndCopy(ref KnownTechFlags, Bic.Tech.Length);
                            AllocateAndCopy(ref GreatWonderCityIDs, Bic.Bldg.Length);
                            AllocateAndCopy(ref GreatWondersBuilt, Bic.Bldg.Length);
                            break;
                        case 0x444c5257: // WRLD
                            Copy(ref Wrld, sizeof(WRLD));
                            break;
                        case 0x454c4954: // TILE
                            AllocateAndCopy(ref Tile, Wrld.Width * Wrld.Height / 2);
                            break;
                        default:
                            scan++;
                            break;
                    }
                }
            }
        }

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
