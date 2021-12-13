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

        public unsafe BicData(byte[] bicBytes)
        {
            Bic = new Civ3File(bicBytes);

            fixed (byte* bytePtr = bicBytes)
            {
                int offset = 736;
                string header;
                int count = 0;
                int dataLength = 0;

                while (offset < bicBytes.Length - 12) {
                    header = Bic.GetString(offset, 4);
                    count = Bic.ReadInt32(offset + 4);
                    offset += 8;

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
                            dataLength = -7;
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
                            dataLength = -7;
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
                            dataLength = -7;
                            break;
                        case "TECH":
                            dataLength = -7;
                            break;
                        case "TERR":
                            int terrLen = Bic.ReadInt32(offset) + 4;
                            dataLength = count * terrLen;
                            Terr = new TERR[count];
                            fixed (void* ptr = Terr) {
                                byte* terrBytePtr = (byte*)ptr;
                                for (int i = 0; i < count; i++) {
                                    Buffer.MemoryCopy(bytePtr + offset + i * terrLen, terrBytePtr, 8, 8);
                                    Buffer.MemoryCopy(bytePtr + offset + (i + 1) * terrLen - sizeof(TERR) + 8, terrBytePtr + 8, sizeof(TERR) - 8, sizeof(TERR) - 8);
                                    terrBytePtr += sizeof(TERR);
                                }
                            }
                            break;
                        case "TFRM":
                            dataLength = -7;
                            break;
                        case "TILE":
                            dataLength = -7;
                            break;
                        case "UNIT":
                            dataLength = -7;
                            break;
                        case "WCHR":
                            dataLength = -7;
                            break;
                        case "WMAP":
                            dataLength = -7;
                            break;
                        case "WSIZ":
                            dataLength = -7;
                            break;
                        default:
                            dataLength = -7;
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
