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
        public CONT[] Cont;
        public LEAD[] Lead;

        public int[] CitiesPerContinent;
        public IntBitmap[] KnownTechFlags;
        public int[] GreatWonderCityIDs;
        public bool[] GreatWondersBuilt;
        public int[] ResourceCounts;
        public LEAD_LEAD[][] ReputationRelationship;
        public LEAD_LEAD_Diplomacy[,][] LeadLeadDiplomacy;

        public int[][] LeadTechQueue;

        private const int SECTION_HEADERS_START = 736;

        private const int LEAD_LEN_1 = 412;
        private const int LEAD_LEN_2 = 2696;
        private const int LEAD_LEN_3 = 108;
        private const int LEAD_LEN_4 = 292;

        public SavData(byte[] savBytes, byte[] biqBytes)
        {
            Bic = new BiqData(biqBytes);
            Load(savBytes);
        }

        public unsafe void Copy<T>(ref T data, int length, int offset = 0) where T : unmanaged
        {
            fixed (void* destPtr = &data) {
                Buffer.MemoryCopy(scan, (byte*)destPtr + offset, length, length);
            }
            scan += length;
        }

        public unsafe void CopyArray<T>(ref T[] data, int length) where T : unmanaged
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
                            CopyArray(ref CitiesPerContinent, Game.NumberOfContinents);
                            CopyArray(ref KnownTechFlags, Bic.Tech.Length);
                            CopyArray(ref GreatWonderCityIDs, Bic.Bldg.Length);
                            CopyArray(ref GreatWondersBuilt, Bic.Bldg.Length);
                            break;
                        case 0x444c5257: // WRLD
                            Copy(ref Wrld, sizeof(WRLD));
                            break;
                        case 0x454c4954: // TILE
                            CopyArray(ref Tile, Wrld.Width * Wrld.Height / 2);
                            break;
                        case 0x544e4f43: // CONT
                            CopyArray(ref Cont, Game.NumberOfContinents);
                            CopyArray(ref ResourceCounts, Bic.Good.Length);
                            break;
                        case 0x4441454c: // LEAD
                            const int LEAD_COUNT = 32;
                            Lead = new LEAD[LEAD_COUNT];
                            ReputationRelationship = new LEAD_LEAD[LEAD_COUNT][];
                            LeadLeadDiplomacy = new LEAD_LEAD_Diplomacy[LEAD_COUNT, LEAD_COUNT][];

                            for (int i = 0; i < 32; i++) {
                                Copy(ref Lead[i], LEAD_LEN_1);
                                CopyArray(ref ReputationRelationship[i], LEAD_COUNT);
                                Copy(ref Lead[i], LEAD_LEN_2, LEAD_LEN_1);

                                for (int j = 0; j < 32; j++) {
                                    header = (int*)scan;
                                    scan += 4;
                                    CopyArray(ref LeadLeadDiplomacy[i, j], *header);
                                }

                                if (Lead[i].RaceID != -1) {
                                    scan += Bic.Bldg.Length * 11;
                                    scan += Bic.Prto.Length * 6;
                                    scan += Bic.Rule[0].NumberOfSpaceshipParts * 2;
                                    scan += Bic.Good.Length * 32 * 3;
                                    scan += Bic.Good.Length;
                                    scan += Wrld.ContinentCount * 20;
                                }

                                Copy(ref Lead[i], LEAD_LEN_3, LEAD_LEN_1 + LEAD_LEN_2);
                                scan += Lead[i].ScienceQueueSize * 4;
                                Copy(ref Lead[i], LEAD_LEN_4, LEAD_LEN_1 + LEAD_LEN_2 + LEAD_LEN_3);
                            }
                            break;
                        default:
                            scan++;
                            break;
                    }
                }
            }
        }

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
