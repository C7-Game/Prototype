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

        public bool[,] TerrGood; // which resources are allowed on which types of terrain
        public GOVTGOVT[,] GovtGovt; // relationships between governments

        public unsafe BicData(byte[] bicBytes)
        {
            Bic = new Civ3File(bicBytes);

            fixed (byte* bytePtr = bicBytes)
            {
                int offset = 736; // byte index of the first header in BIQ files
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
                            dataLength = -7;
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
                                    Buffer.MemoryCopy(dataPtr, govtPtr, 400, 400);
                                    Buffer.MemoryCopy(dataPtr + 400, govtgovtPtr, govtgovtRowLength, govtgovtRowLength);
                                    Buffer.MemoryCopy(dataPtr + 400 + govtgovtRowLength, govtPtr + 400, 76, 76);
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
                            dataLength = -7;
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
                                // TERR contains dynamic data, so it can't be read in as a block. Instead, read in data for each TERR
                                for (int i = 0; i < count; i++) {
                                    // Copy first 8 bytes into TERR struct:
                                    Buffer.MemoryCopy(bytePtr + offset + i * terrLen, terrBytePtr, 8, 8);
                                    // Get TerrGood flags, dynamic data which determines which resources are allowed on which terrain types
                                    for (int j = 0; j < goodCount; j++) {
                                        TerrGood[i,j] = Util.GetFlag(*(bytePtr + offset + i * terrLen + 8 + (j / 8)), j % 8);
                                    }
                                    // Copy remaining bytes into TERR struct:
                                    Buffer.MemoryCopy(bytePtr + offset + (i + 1) * terrLen - sizeof(TERR) + 8, terrBytePtr + 8, sizeof(TERR) - 8, sizeof(TERR) - 8);
                                    terrBytePtr += sizeof(TERR);
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
