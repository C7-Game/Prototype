using System;
using System.IO;
using System.Collections.Generic;
using Blast;

namespace ReadCivData.QueryCiv3Sav {
    public class Civ3Section {
        public string Name;
        public int Offset;
    }
    public class Civ3File {
        protected internal byte[] FileData;
        protected internal Civ3Section[] Sections;
        public void Load(string pathName) {
            this.FileData = File.ReadAllBytes(pathName);
            if (FileData[0] == 0x00 && (FileData[1] == 0x04 || FileData[1] == 0x05 || FileData[1] == 0x06)) {
                this.Decompress();
            }
            // TODO: Check for CIV3 or BIC header?
            this.PopulateSections();
        }
        // For dev validation only
        public void PrintFirstFourBytes() {
            System.Text.ASCIIEncoding ascii = new System.Text.ASCIIEncoding();
            Console.WriteLine(ascii.GetString(this.FileData, 0, 4));
        }
        protected internal void Decompress() {
            MemoryStream DecompressedStream = new MemoryStream();
            BlastDecoder Decompressor = new BlastDecoder(new MemoryStream(this.FileData, writable: false), DecompressedStream);
            Decompressor.Decompress();
            this.FileData = DecompressedStream.ToArray();
        }
        protected internal void PopulateSections() {
            int Count = 0;
            int Offset = 0;
            List<Civ3Section> SectionList = new List<Civ3Section>();
            System.Text.ASCIIEncoding ascii = new System.Text.ASCIIEncoding();
            for (int i = 0; i < this.FileData.Length; i++) {
                if (FileData[i] < 0x20 || FileData[i] > 0x5a) {
                    Count = 0;
                } else {
                    if (Count == 0) {
                        Offset = i;
                    }
                    Count++;
                }
                if (Count > 3) {
                    Count = 0;
                    Civ3Section Section = new Civ3Section();
                    Section.Offset = Offset;
                    Section.Name = ascii.GetString(this.FileData, Offset, 4);
                    SectionList.Add(Section);
                    // Console.WriteLine(Section.Name + " " + Section.Offset);
                }
            }
            // TODO: Filter junk and dirty data from array (e.g. stray CITYs, non-headers, and such)
            this.Sections = SectionList.ToArray();
        }
        public int SectionOffset(string name, int nth) {
            int n = 0;
            for (int i = 0; i < this.Sections.Length; i++) {
                if (this.Sections[i].Name == name) {
                    n++;
                    if (n >= nth) {
                        return this.Sections[i].Offset + name.Length;
                    }
                }
            }
            // TODO: Add name and nth to message
            throw new ArgumentException("Unable to find section");
        }
        // TODO: Force little endian conversion on big endian systems
        //  although anticipated Intel and ARM targets are little endian, so maybe not important
        // TODO: Review default values of signed bool
        public int ReadInt32(int offset, bool signed = true) {
            if (signed) {
                return BitConverter.ToInt32(this.FileData, offset);
            }
            return (int)BitConverter.ToUInt32(this.FileData, offset);
        }
        public int ReadInt16(int offset, bool signed = false) {
            if (signed) {
                return BitConverter.ToInt16(this.FileData, offset);
            }
            return (int)BitConverter.ToUInt16(this.FileData, offset);
        }
        // TODO: Figure out if signed byte is needed anywhere
        public int ReadByte(int offset) => (int)this.FileData[offset];
    }
}