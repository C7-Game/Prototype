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

            fixed (byte* bytePtr = bicBytes) {
                int offset = 736;
                while (offset < bicBytes.Length) {
                    string header = Bic.GetString(offset, 4);
                    switch (header) {
                        case "BLDG":
                            int buildingCount = Bic.ReadInt32(offset + 4);
                            offset += 8;
                            int dataLength = buildingCount * sizeof(BLDG);
                            Console.WriteLine("{0} {1}", buildingCount, dataLength);
                            Bldg = new BLDG[buildingCount];
                            fixed (BLDG* bldgPtr = Bldg) {
                                Buffer.MemoryCopy(bytePtr + offset, bldgPtr, dataLength, dataLength);
                            }
                            offset += dataLength;
                            break;
                        default:
                            offset = bicBytes.Length; // exit loop
                            break;
                    }
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
