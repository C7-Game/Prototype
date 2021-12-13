using System;
using System.Runtime.InteropServices;

namespace QueryCiv3.Biq
{
    [StructLayout(LayoutKind.Sequential, Pack=1)]
    public unsafe struct CULT
    {
        public int Length;

        private fixed byte Text[64];
        public string CultureOpinionName {
            get {
                var Entry = new byte[64];
                fixed (byte* p1 = Text, p2 = Entry) {
                    Buffer.MemoryCopy(p1, p2, 64, 64);
                }
                return Util.GetString(Entry);
            }
        }

        public int ChanceOfSuccessfulPropaganda;
        public int CultureRatioPercentage;
        public int CultureRatioDenominator;
        public int CultureRatioNumerator;
        public int InitialResistanceChance;
        public int ContinuedResistanceChance;
    }
}
