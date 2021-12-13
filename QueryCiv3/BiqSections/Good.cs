using System;
using System.Runtime.InteropServices;

namespace QueryCiv3.Biq
{
    [StructLayout(LayoutKind.Sequential, Pack=1)]
    public unsafe struct GOOD
    {
        public int Length;

        private fixed byte Text[56];
        public string ResourceName {
            get {
                var Entry = new byte[24];
                fixed (byte* p1 = Text, p2 = Entry) {
                    Buffer.MemoryCopy(p1, p2, 24, 24);
                }
                return Util.GetString(Entry);
            }
        }
        public string CivilopediaEntry {
            get {
                var Entry = new byte[32];
                fixed (byte* p1 = Text, p2 = Entry) {
                    Buffer.MemoryCopy(p1 + 24, p2, 32, 32);
                }
                return Util.GetString(Entry);
            }
        }

        public int Type;
        public int AppearanceRatio;
        public int DisappearanceProbability;
        public int Icon;
        public int Prerequisite;
        public int FoodBonus;
        public int ShieldsBonus;
        public int CommerceBonus;
    }
}
