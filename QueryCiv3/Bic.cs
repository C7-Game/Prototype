using System;
using System.Collections.Generic;
using QueryCiv3.Biq;

namespace QueryCiv3
{
    public class BicData
    {
        public Civ3File Bic;
        public bool HasCustomRules => Bic.SectionExists("BLDG");
        public bool HasCustomMap => Bic.SectionExists("WCHR");

        public BLDG[] Bldg;
        public CITY[] City;
        public CLNY[] Clny;
        public CONT[] Cont;
        public CTZN[] Ctzn;
        public CULT[] Cult;
        public DIFF[] Diff;
        public ERAS[] Eras;
        public ESPN[] Espn;
        public EXPR[] Expr;
        public FLAV[] Flav;
        public GAME[] Game;
        public GOOD[] Good;
        public GOVT[] Govt;
        public LEAD[] Lead;
        public PRTO[] Prto;
        public RACE[] Race;
        public RULE[] Rule;
        public SLOC[] Sloc;
        public TECH[] Tech;
        public TERR[] Terr;
        public TFRM[] Tfrm;
        public TILE[] Tile;
        public UNIT[] Unit;
        public WCHR[] Wchr;
        public WMAP[] Wmap;
        public WSIZ[] Wsiz;

        /*
            Note which of the following are rectangular 2d arrays [,] vs which are jagged 2d arrays [][]
            A rectangular array means that the second dimension for all components is the same eg. for RaceEra, every civ shares the same # of Eras
            A jagged array means that the second dimension can vary between components eg. for RaceCityName, each civ can have a different # of Cities
        */
        public bool[,] TerrGood; // which resources are allowed on which types of terrain
        public GOVTGOVT[,] GovtGovt; // relationships between governments
        public RACECITYNAME[][] RaceCityName; // the list of city names for each civ
        public RACEERA[,] RaceEra; // the file names for each era for each civ
        public RACELEADERNAME[][] RaceGreatLeaderName; // the great leaders for each civ
        public RACELEADERNAME[][] RaceScientificLeaderName; // the scientific leaders for each civ
        public int[][] CityBuilding;

        private const int SECTION_HEADERS_START = 736;
        // Dynamic sections need to have their static subcomponents read in as discrete chunks, which these length constants help with
        // The sum of the LEN constants for each section equals the total size of that section's struct
        // eg. GOVT_LEN_1 + GOVT_LEN_2 == sizeof(GOVT)
        private const int GOVT_LEN_1 = 400;
        private const int GOVT_LEN_2 = 76;
        private const int TERR_LEN_1 = 8;
        private const int TERR_LEN_2 = 225;
        private const int RACE_LEN_1 = 8;
        private const int RACE_LEN_2 = 4;
        private const int RACE_LEN_3 = 208;
        private const int RACE_LEN_4 = 92;
        private const int CITY_LEN_1 = 38;
        private const int CITY_LEN_2 = 36;

        public unsafe BicData(byte[] bicBytes)
        {
            Bic = new Civ3File(bicBytes);

            fixed (byte* bytePtr = bicBytes)
            {
                // For now, we're skipping over the VER# and BIQ file header information to get right to the structs
                // The first section is likely to be BLDG in BIQ files, but the current approach supports any ordering of the sections
                int offset = SECTION_HEADERS_START;
                string header;
                int count = 0;
                int dataLength = 0;

                while (offset < bicBytes.Length - 12) { // Don't read past the end
                    // We don't know what orders the headers come in or which headers will be set, so get the next header and switch off it:
                    header = Bic.GetString(offset, 4);
                    count = Bic.ReadInt32(offset + 4);
                    offset += 8;

                    // Section data structures are stored in the BiqSections/ folder
                    // We can divide the BIQ sections into two types: static and dynamic
                    // Static have a fixed length always, which means they can be read directly memory-copied into our structs with no special logic
                    //   The static sections are: BLDG, CTZN, CULT, DIFF, ERAS, ESPN, EXPR, GOOD, TECH, TFRM, WSIZ, WCHR, TILE, CONT, SLOC, UNIT, CLNY
                    // Dynamic sections have at least one component with varying length, and so require multiple structs and special logic
                    //   The dynamic sections are: GOVT, RULE, PRTO, RACE, TERR, FLAV, WMAP, CITY, GAME, LEAD
                    switch (header) {
                        case "BLDG":
                            dataLength = count * sizeof(BLDG);
                            Bldg = new BLDG[count];
                            fixed (void* ptr = Bldg) {
                                Buffer.MemoryCopy(bytePtr + offset, ptr, dataLength, dataLength);
                            }
                            break;
                        case "CITY":
                            dataLength = 0;
                            City = new CITY[count];
                            CityBuilding = new int[count][];
                            int buildingRowLength = 0;

                            fixed (void* ptr = City) {
                                byte* cityPtr = (byte*)ptr;
                                byte* dataPtr = bytePtr + offset;

                                for (int i = 0; i < count; i++) {
                                    Buffer.MemoryCopy(dataPtr, cityPtr, CITY_LEN_1, CITY_LEN_1);
                                    CityBuilding[i] = new int[City[i].NumberOfBuildings];
                                    buildingRowLength = City[i].NumberOfBuildings * sizeof(int);
                                    fixed (void* ptr2 = CityBuilding[i]) Buffer.MemoryCopy(dataPtr + CITY_LEN_1, ptr2, buildingRowLength, buildingRowLength);
                                    Buffer.MemoryCopy(dataPtr + CITY_LEN_1 + buildingRowLength, cityPtr + CITY_LEN_1, CITY_LEN_2, CITY_LEN_2);

                                    cityPtr += sizeof(CITY);
                                    dataPtr += City[i].Length + 4;
                                    dataLength += City[i].Length + 4;
                                }
                            }
                            break;
                        case "CLNY":
                            dataLength = count * sizeof(CLNY);
                            Clny = new CLNY[count];
                            fixed (void* ptr = Clny) {
                                Buffer.MemoryCopy(bytePtr + offset, ptr, dataLength, dataLength);
                            }
                            break;
                        case "CONT":
                            dataLength = -7;
                            break;
                        case "CTZN":
                            dataLength = count * sizeof(CTZN);
                            Ctzn = new CTZN[count];
                            fixed (void* ptr = Ctzn) {
                                Buffer.MemoryCopy(bytePtr + offset, ptr, dataLength, dataLength);
                            }
                            break;
                        case "CULT":
                            dataLength = count * sizeof(CULT);
                            Cult = new CULT[count];
                            fixed (void* ptr = Cult) {
                                Buffer.MemoryCopy(bytePtr + offset, ptr, dataLength, dataLength);
                            }
                            break;
                        case "DIFF":
                            dataLength = count * sizeof(DIFF);
                            Diff = new DIFF[count];
                            fixed (void* ptr = Diff) {
                                Buffer.MemoryCopy(bytePtr + offset, ptr, dataLength, dataLength);
                            }
                            break;
                        case "ERAS":
                            dataLength = count * sizeof(ERAS);
                            Eras = new ERAS[count];
                            fixed (void* ptr = Eras) {
                                Buffer.MemoryCopy(bytePtr + offset, ptr, dataLength, dataLength);
                            }
                            break;
                        case "ESPN":
                            dataLength = count * sizeof(ESPN);
                            Espn = new ESPN[count];
                            fixed (void* ptr = Espn) {
                                Buffer.MemoryCopy(bytePtr + offset, ptr, dataLength, dataLength);
                            }
                            break;
                        case "EXPR":
                            dataLength = count * sizeof(EXPR);
                            Expr = new EXPR[count];
                            fixed (void* ptr = Expr) {
                                Buffer.MemoryCopy(bytePtr + offset, ptr, dataLength, dataLength);
                            }
                            break;
                        case "FLAV":
                            dataLength = -7;
                            break;
                        case "GAME":
                            dataLength = -7;
                            break;
                        case "GOOD":
                            dataLength = count * sizeof(GOOD);
                            Good = new GOOD[count];
                            fixed (void* ptr = Good) {
                                Buffer.MemoryCopy(bytePtr + offset, ptr, dataLength, dataLength);
                            }
                            break;
                        case "GOVT":
                            int govtLen = Bic.ReadInt32(offset) + 4;
                            dataLength = count * govtLen;
                            Govt = new GOVT[count];
                            GovtGovt = new GOVTGOVT[count, count];
                            int govtgovtRowLength = count * sizeof(GOVTGOVT);

                            fixed (void* ptr = Govt, ptr2 = GovtGovt) {
                                byte* govtPtr = (byte*)ptr;
                                byte* govtgovtPtr = (byte*)ptr2;
                                byte* dataPtr = bytePtr + offset;

                                for (int i = 0; i < count; i++) {
                                    Buffer.MemoryCopy(dataPtr, govtPtr, GOVT_LEN_1, GOVT_LEN_1);
                                    Buffer.MemoryCopy(dataPtr + GOVT_LEN_1, govtgovtPtr, govtgovtRowLength, govtgovtRowLength);
                                    Buffer.MemoryCopy(dataPtr + GOVT_LEN_1 + govtgovtRowLength, govtPtr + GOVT_LEN_1, GOVT_LEN_2, GOVT_LEN_2);
                                    govtPtr += sizeof(GOVT);
                                    govtgovtPtr += govtgovtRowLength;
                                    dataPtr += govtLen;
                                }
                            }
                            break;
                        case "LEAD":
                            dataLength = -7;
                            break;
                        case "PRTO":
                            dataLength = -7;
                            break;
                        case "RACE":
                            dataLength = 0;
                            Race = new RACE[count];
                            RaceCityName = new RACECITYNAME[count][];
                            RaceScientificLeaderName = new RACELEADERNAME[count][];
                            RaceGreatLeaderName = new RACELEADERNAME[count][];
                            /*
                                For getting dynamic race data, we need to know the number of eras as defined earlier
                                Presumably this means that the ERAS section of BIQ will always appear before the RACE section
                                However, if ERAS ever appears after RACE, then that will be a problem
                                I considered throwing an exception here in that case, but C# will throw a NullReferenceException anyway for
                                  trying to get the length of an uninitialized array, which is sufficient
                            */
                            int eras = Eras.Length;
                            RaceEra = new RACEERA[count, eras];
                            int raceeraRowLength = eras * sizeof(RACEERA);
                            int rowLength = 0;

                            fixed (void* ptr = Race, ptr2 = RaceEra) {
                                byte* racePtr = (byte*)ptr;
                                byte* raceeraPtr = (byte*)ptr2;
                                byte* dataPtr = bytePtr + offset;

                                for (int i = 0; i < count; i++) {
                                    Buffer.MemoryCopy(dataPtr, racePtr, RACE_LEN_1, RACE_LEN_1);
                                    racePtr += RACE_LEN_1;
                                    RaceCityName[i] = new RACECITYNAME[Race[i].NumberOfCities];
                                    rowLength = Race[i].NumberOfCities * sizeof(RACECITYNAME);
                                    fixed (void* ptr3 = RaceCityName[i]) Buffer.MemoryCopy(dataPtr + RACE_LEN_1, ptr3, rowLength, rowLength);
                                    dataPtr += RACE_LEN_1 + rowLength;

                                    Buffer.MemoryCopy(dataPtr, racePtr, RACE_LEN_2, RACE_LEN_2);
                                    racePtr += RACE_LEN_2;
                                    RaceGreatLeaderName[i] = new RACELEADERNAME[Race[i].NumberOfGreatLeaders];
                                    rowLength = Race[i].NumberOfGreatLeaders * sizeof(RACELEADERNAME);
                                    fixed (void* ptr3 = RaceGreatLeaderName[i]) Buffer.MemoryCopy(dataPtr + RACE_LEN_2, ptr3, rowLength, rowLength);
                                    dataPtr += RACE_LEN_2 + rowLength;

                                    Buffer.MemoryCopy(dataPtr, racePtr, RACE_LEN_3, RACE_LEN_3);
                                    racePtr += RACE_LEN_3;
                                    Buffer.MemoryCopy(dataPtr + RACE_LEN_3, raceeraPtr, raceeraRowLength, raceeraRowLength);
                                    dataPtr += RACE_LEN_3 + raceeraRowLength;
                                    raceeraPtr += raceeraRowLength;

                                    Buffer.MemoryCopy(dataPtr, racePtr, RACE_LEN_4, RACE_LEN_4);
                                    racePtr += RACE_LEN_4;
                                    RaceScientificLeaderName[i] = new RACELEADERNAME[Race[i].NumberOfScientificLeaders];
                                    rowLength = Race[i].NumberOfScientificLeaders * sizeof(RACELEADERNAME);
                                    fixed (void* ptr3 = RaceScientificLeaderName[i]) Buffer.MemoryCopy(dataPtr + RACE_LEN_4, ptr3, rowLength, rowLength);
                                    dataPtr += RACE_LEN_4 + rowLength;

                                    dataLength += Race[i].Length + 4;
                                }
                            }

                            break;
                        case "RULE":
                            dataLength = -7;
                            break;
                        case "SLOC":
                            dataLength = count * sizeof(SLOC);
                            Sloc = new SLOC[count];
                            fixed (void* ptr = Sloc) {
                                Buffer.MemoryCopy(bytePtr + offset, ptr, dataLength, dataLength);
                            }
                            break;
                        case "TECH":
                            dataLength = count * sizeof(TECH);
                            Tech = new TECH[count];
                            fixed (void* ptr = Tech) {
                                Buffer.MemoryCopy(bytePtr + offset, ptr, dataLength, dataLength);
                            }
                            break;
                        case "TERR":
                            int terrLen = Bic.ReadInt32(offset) + 4; // Add 4 because length must also include the 32-bit integer that is itself
                            dataLength = count * terrLen;
                            Terr = new TERR[count];
                            int goodCount = Bic.ReadInt32(offset + 4);
                            TerrGood = new bool[count, goodCount];

                            fixed (void* ptr = Terr) {
                                byte* terrBytePtr = (byte*)ptr;
                                byte* dataPtr = bytePtr + offset;

                                // TERR contains dynamic data, so it can't be read in as a block. Instead, read in data for each TERR
                                for (int i = 0; i < count; i++) {
                                    Buffer.MemoryCopy(dataPtr, terrBytePtr, TERR_LEN_1, TERR_LEN_1);
                                    dataPtr += TERR_LEN_1;

                                    // Get TerrGood flags, dynamic data which determines which resources are allowed on which terrain types
                                    for (int j = 0; j < goodCount; j++) {
                                        TerrGood[i, j] = Util.GetFlag(*dataPtr, j % 8);
                                        // Incrememt byte position every 8th bit read or after all bits read:
                                        if (j % 8 == 7 || j == goodCount - 1) dataPtr++;
                                    }

                                    Buffer.MemoryCopy(dataPtr, terrBytePtr + TERR_LEN_1, TERR_LEN_2, TERR_LEN_2);
                                    terrBytePtr += sizeof(TERR);
                                    dataPtr += TERR_LEN_2;
                                }
                            }
                            break;
                        case "TFRM":
                            dataLength = count * sizeof(TFRM);
                            Tfrm = new TFRM[count];
                            fixed (void* ptr = Tfrm) {
                                Buffer.MemoryCopy(bytePtr + offset, ptr, dataLength, dataLength);
                            }
                            break;
                        case "TILE":
                            dataLength = count * sizeof(TILE);
                            Tile = new TILE[count];
                            fixed (void* ptr = Tile) {
                                Buffer.MemoryCopy(bytePtr + offset, ptr, dataLength, dataLength);
                            }
                            break;
                        case "UNIT":
                            dataLength = count * sizeof(UNIT);
                            Unit = new UNIT[count];
                            fixed (void* ptr = Unit) {
                                Buffer.MemoryCopy(bytePtr + offset, ptr, dataLength, dataLength);
                            }
                            break;
                        case "WCHR":
                            dataLength = count * sizeof(WCHR);
                            Wchr = new WCHR[count];
                            fixed (void* ptr = Wchr) {
                                Buffer.MemoryCopy(bytePtr + offset, ptr, dataLength, dataLength);
                            }
                            break;
                        case "WMAP":
                            dataLength = -7;
                            break;
                        case "WSIZ":
                            dataLength = count * sizeof(WSIZ);
                            Wsiz = new WSIZ[count];
                            fixed (void* ptr = Wsiz) {
                                Buffer.MemoryCopy(bytePtr + offset, ptr, dataLength, dataLength);
                            }
                            break;
                        default:
                            // Once every BIQ section is set up with the correct associate struct(s), the default case where a header isn't found
                            // should never occur. However, for now, if a header isn't found, we'll just increment by 1 to keep searching
                            dataLength = -7; // We added 8 earlier, so subtract 7
                            break;
                    }
                    offset += dataLength;
                }
            }
        }
        public string Title => Bic.GetString(0x2a0, 64);
        // unsure of this length ... up to 656
        public string Description => Bic.GetString(0x20, 640);
        public string RelativeModPath
        {
            get
            {
                try
                {
                    int gameOff = Bic.SectionOffset("GAME", 1);
                    // I don't know if this length is correct, just guessing
                    string output = Bic.GetString(gameOff + 0xdc, 256);
                    if (output != "") return output;
                }
                catch {}
                return Title;
            }
        }
    }
}
