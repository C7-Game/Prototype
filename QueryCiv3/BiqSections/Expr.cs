using System;
using System.Runtime.InteropServices;

namespace QueryCiv3.Biq
{
    [StructLayout(LayoutKind.Sequential, Pack=1)]
    public unsafe struct EXPR
    {
        public int Length;

        private fixed byte Text[32];
        public string ExperienceLevelName {
            get {
                var Entry = new byte[32];
                fixed (byte* p1 = Text, p2 = Entry) {
                    Buffer.MemoryCopy(p1, p2, 32, 32);
                }
                return Util.GetString(Entry);
            }
        }

        public int BaseHitPoints;
        public int RetreatBonus;
    }
}
