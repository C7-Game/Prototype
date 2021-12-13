using System;
using System.Runtime.InteropServices;

namespace QueryCiv3.Biq
{
    [StructLayout(LayoutKind.Sequential, Pack=1)]
    public unsafe struct CTZN
    {
        public int Length;
        public int DefaultCitizen;

        private fixed byte Text[96];
        public string SingularName {
            get {
                var Entry = new byte[32];
                fixed (byte* p1 = Text, p2 = Entry) {
                    Buffer.MemoryCopy(p1, p2, 32, 32);
                }
                return Util.GetString(Entry);
            }
        }
        public string CivilopediaEntry {
            get {
                var Entry = new byte[32];
                fixed (byte* p1 = Text, p2 = Entry) {
                    Buffer.MemoryCopy(p1 + 32, p2, 32, 32);
                }
                return Util.GetString(Entry);
            }
        }
        public string PluralName {
            get {
                var Entry = new byte[32];
                fixed (byte* p1 = Text, p2 = Entry) {
                    Buffer.MemoryCopy(p1 + 64, p2, 32, 32);
                }
                return Util.GetString(Entry);
            }
        }

        public int Prerequisite;
        public int Luxuries;
        public int Research;
        public int Taxes;
        public int Corruption;
        public int Construction;
    }
}
