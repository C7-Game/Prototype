using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace QueryCiv3
{
    public class BldgSection : SectionListItem
    {
        public string Name { get => Bic.GetString(Offset+64, 32); }
        // TODO: I'm sure this is a dumb name
        public string Reference { get => Bic.GetString(Offset+96, 32); }
    }
    public class CtznSection : SectionListItem {}
    public class CultSection : SectionListItem {}
    public class DiffSection : SectionListItem {}
    public class ErasSection : SectionListItem {}
    public class EspnSection : SectionListItem {}
    public class ExprSection : SectionListItem {}
    public class GoodSection : SectionListItem {}
    public class GovtSection : SectionListItem {}
    public class PrtoSection : SectionListItem {}
    public class RaceSection : SectionListItem {}
    public class TechSection : SectionListItem {}
    public class TfrmSection : SectionListItem {}
    public class TerrSection : SectionListItem {}
    public class WsizSection : SectionListItem {}
    public class FlavSection : SectionListItem {}

}
