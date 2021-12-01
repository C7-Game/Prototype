using System;
using System.Collections.Generic;

namespace QueryCiv3
{
    public class BicData
    {
        public Civ3File Bic;
        public bool HasCustomRules => Bic.SectionExists("BLDG");
        public bool HasCustomMap => Bic.SectionExists("WCHR");

        public BicData(byte[] bicBytes)
        {
            Bic = new Civ3File(bicBytes);
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
        public BldgSection[] Bldg { get => (new ListSection<BldgSection>(Bic, Bic.SectionOffset("BLDG", 1))).Sections.ToArray(); }
        public CtznSection[] Ctzn { get => (new ListSection<CtznSection>(Bic, Bic.SectionOffset("CTZN", 1))).Sections.ToArray(); }
        public CultSection[] Cult { get => (new ListSection<CultSection>(Bic, Bic.SectionOffset("CULT", 1))).Sections.ToArray(); }
        public DiffSection[] Diff { get => (new ListSection<DiffSection>(Bic, Bic.SectionOffset("DIFF", 1))).Sections.ToArray(); }
        public ErasSection[] Eras { get => (new ListSection<ErasSection>(Bic, Bic.SectionOffset("ERAS", 1))).Sections.ToArray(); }
        public EspnSection[] Espn { get => (new ListSection<EspnSection>(Bic, Bic.SectionOffset("ESPN", 1))).Sections.ToArray(); }
        public ExprSection[] Expr { get => (new ListSection<ExprSection>(Bic, Bic.SectionOffset("EXPR", 1))).Sections.ToArray(); }
        public GoodSection[] Good { get => (new ListSection<GoodSection>(Bic, Bic.SectionOffset("GOOD", 1))).Sections.ToArray(); }
        public GovtSection[] Govt { get => (new ListSection<GovtSection>(Bic, Bic.SectionOffset("GOVT", 1))).Sections.ToArray(); }
        public PrtoSection[] Prto { get => (new ListSection<PrtoSection>(Bic, Bic.SectionOffset("PRTO", 1))).Sections.ToArray(); }
        public RaceSection[] Race { get => (new ListSection<RaceSection>(Bic, Bic.SectionOffset("RACE", 1))).Sections.ToArray(); }
        public TechSection[] Tech { get => (new ListSection<TechSection>(Bic, Bic.SectionOffset("TECH", 1))).Sections.ToArray(); }
        public TfrmSection[] Tfrm { get => (new ListSection<TfrmSection>(Bic, Bic.SectionOffset("TFRM", 1))).Sections.ToArray(); }
        public TerrSection[] Terr { get => (new ListSection<TerrSection>(Bic, Bic.SectionOffset("TERR", 1))).Sections.ToArray(); }
        public WsizSection[] Wsiz { get => (new ListSection<WsizSection>(Bic, Bic.SectionOffset("WSIZ", 1))).Sections.ToArray(); }
        public FlavSection[] Flav { get => (new ListSection<FlavSection>(Bic, Bic.SectionOffset("FLAV", 1))).Sections.ToArray(); }
    }
}