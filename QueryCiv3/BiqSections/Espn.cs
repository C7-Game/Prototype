using System;
using System.Runtime.InteropServices;

namespace QueryCiv3.Biq
{
    [StructLayout(LayoutKind.Sequential, Pack=1)]
    public unsafe struct ESPN
    {
        public int Length;

        private fixed byte Text[224];
        public string Description {
            get {
                var Entry = new byte[128];
                fixed (byte* p1 = Text, p2 = Entry) {
                    Buffer.MemoryCopy(p1, p2, 128, 128);
                }
                return Util.GetString(Entry);
            }
        }
        public string MissionName {
            get {
                var Entry = new byte[64];
                fixed (byte* p1 = Text, p2 = Entry) {
                    Buffer.MemoryCopy(p1 + 128, p2, 64, 64);
                }
                return Util.GetString(Entry);
            }
        }
        public string CivilopediaEntry {
            get {
                var Entry = new byte[32];
                fixed (byte* p1 = Text, p2 = Entry) {
                    Buffer.MemoryCopy(p1 + 192, p2, 32, 32);
                }
                return Util.GetString(Entry);
            }
        }

        private fixed byte Flags[4];
        public bool Diplomat { get => Util.GetFlag(Flags[0], 0); }
        public bool Spy      { get => Util.GetFlag(Flags[0], 1); }

        public int BaseCost;
    }
}
