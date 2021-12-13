using System;
using System.Runtime.InteropServices;

namespace QueryCiv3.Biq
{
    [StructLayout(LayoutKind.Sequential, Pack=1)]
    public unsafe struct ERAS
    {
        public int Length;

        private fixed byte Text[256];
        public string EraName {
            get {
                var Entry = new byte[64];
                fixed (byte* p1 = Text, p2 = Entry) {
                    Buffer.MemoryCopy(p1, p2, 64, 64);
                }
                return Util.GetString(Entry);
            }
        }
        public string CivilopediaEntry {
            get {
                var Entry = new byte[32];
                fixed (byte* p1 = Text, p2 = Entry) {
                    Buffer.MemoryCopy(p1 + 64, p2, 32, 32);
                }
                return Util.GetString(Entry);
            }
        }
        public string Researcher1 {
            get {
                var Entry = new byte[32];
                fixed (byte* p1 = Text, p2 = Entry) {
                    Buffer.MemoryCopy(p1 + 96, p2, 32, 32);
                }
                return Util.GetString(Entry);
            }
        }
        public string Researcher2 {
            get {
                var Entry = new byte[32];
                fixed (byte* p1 = Text, p2 = Entry) {
                    Buffer.MemoryCopy(p1 + 128, p2, 32, 32);
                }
                return Util.GetString(Entry);
            }
        }
        public string Researcher3 {
            get {
                var Entry = new byte[32];
                fixed (byte* p1 = Text, p2 = Entry) {
                    Buffer.MemoryCopy(p1 + 160, p2, 32, 32);
                }
                return Util.GetString(Entry);
            }
        }
        public string Researcher4 {
            get {
                var Entry = new byte[32];
                fixed (byte* p1 = Text, p2 = Entry) {
                    Buffer.MemoryCopy(p1 + 192, p2, 32, 32);
                }
                return Util.GetString(Entry);
            }
        }
        public string Researcher5 {
            get {
                var Entry = new byte[32];
                fixed (byte* p1 = Text, p2 = Entry) {
                    Buffer.MemoryCopy(p1 + 224, p2, 32, 32);
                }
                return Util.GetString(Entry);
            }
        }

        public int NumberOfUsedResearcherNames;
        private fixed byte Unknown[4];
    }
}
