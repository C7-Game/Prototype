using System;
using System.Collections.Generic;

namespace QueryCiv3
{
    public class Civ3Section {
        public string Name;
        public int Offset;
    }
    public class Civ3File {
        protected internal byte[] FileData;
        public Civ3Section[] Sections { get; protected set; }
        public bool IsGameFile {get; protected set;}
        public bool IsBicFile {get; protected set;}
        public int Length => FileData.Length;
        public Civ3File(byte[] fileBytes)
        {
            this.FileData = fileBytes;
            Sections = PopulateSections(FileData);
            byte[] Civ3Bytes = new byte[]{0x43, 0x49, 0x56, 0x33};
            byte[] BicBytes = new byte[]{0x42, 0x49, 0x43};
            IsGameFile = true;
            IsBicFile = true;
            for(int i=0; i < 4; i++)
            {
                if(FileData[i] != Civ3Bytes[i])
                {
                    IsGameFile = false;
                }
                if(i < 3 && FileData[i] != BicBytes[i])
                {
                    IsBicFile = false;
                }
            }
        }
        public Boolean SectionExists(string sectionName)
        {
            return Array.Exists(Sections, section => section.Name == sectionName);
        }
        protected internal Civ3Section[] PopulateSections(byte[] Data)
        {
            int Count = 0;
            int Offset = 0;
            List<Civ3Section> MySectionList = new List<Civ3Section>();
            System.Text.ASCIIEncoding ascii = new System.Text.ASCIIEncoding();
            for (int i = 0; i < Data.Length; i++) {
                if (Data[i] < 0x20 || Data[i] > 0x5a) {
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
                    Section.Name = ascii.GetString(Data, Offset, 4);
                    MySectionList.Add(Section);
                }
            }
            // TODO: Filter junk and dirty data from array (e.g. stray CITYs, non-headers, and such)
            return MySectionList.ToArray();
        }
        public int SectionOffset(string name, int nth)
        {
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
            throw new ArgumentException($"Unable to find section '{name}' nth {nth}");
        }
        // TODO: Force little endian conversion on big endian systems
        //  although anticipated Intel and ARM targets are little endian, so maybe not important
        // NOTE: Cast result as (uint) if unsigned desired
        public int ReadInt32(int offset) => BitConverter.ToInt32(this.FileData, offset);
        // NOTE: Cast result as (ushort) if unsigned desired
        public short ReadInt16(int offset) => BitConverter.ToInt16(this.FileData, offset);
        // NOTE: Cast result as (sbyte) if signed desired
        public byte ReadByte(int offset) => this.FileData[offset];
        public byte[] GetBytes(int offset, int length)
        {
            if (offset > Length) return new byte[]{};
            if (offset + length > Length) length = Length - offset;
            List<byte> Out = new List<byte>();
            for(int i=0; i<length; i++) Out.Add(FileData[i+offset]);
            return Out.ToArray();
        }
        // NOTE: Tried to parameterize encoding with default of Civ3StringEncoding, but default must be compile-time constant
        public string GetString(int offset, int length)
        {
            byte[] StringBytes = GetBytes(offset, length);
            return Util.GetString(StringBytes);
        }
    }
}
