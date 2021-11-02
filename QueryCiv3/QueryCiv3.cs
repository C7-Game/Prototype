using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace QueryCiv3
{
    public class Civ3Section {
        public string Name;
        public int Offset;
    }
    public class Civ3File {
        // Encoding code page ID; 1252 is Civ3 encoding for US language version
        //   See https://docs.microsoft.com/en-us/dotnet/api/system.text.encoding?view=net-5.0 for other values
        // NOTE: .NET 5 and later require a Nuget package and Encoder registration for these older encoding pages
        public int Civ3StringEncoding = 1252;
        protected internal byte[] FileData;
        protected internal Civ3Section[] Sections;
        public bool HasCustomBic
        {
            get => (uint)this.ReadInt32(12 + this.SectionOffset("VER#", 1)) != (uint)0xcdcdcdcd;
        }
        public bool IsGameFile {get; protected set;}
        public Civ3File(byte[] fileBytes)
        {
            IsGameFile = false;
            this.FileData = fileBytes;
            // TODO: Check for CIV3 or BIC header?
            Sections = PopulateSections(FileData);
            byte[] Civ3Bytes = new byte[]{0x43, 0x49, 0x56, 0x33};
            IsGameFile = true;
            for(int i=0; i < 4; i++)
            {
                if(FileData[i] != Civ3Bytes[i])
                {
                    IsGameFile = false;
                }
            }
        }
        public byte[] CustomBic
        { get {
            if(HasCustomBic)
            {
                int Start;
                int End;
                try { Start = SectionOffset("BICX", 1); }
                catch
                {
                    try { Start = SectionOffset("BICQ", 1); }
                    catch { Start = SectionOffset("BIC ", 1); }
                }
                // Offset doesn't include section header bytes
                Start -= 4;
                try {
                    End = SectionOffset("GAME", 2) - 4;
                }
                catch { End = this.FileData.Length; }
                List<byte> CustomBic = new List<byte>();
                for(int i=Start; i<End; i++) { CustomBic.Add(FileData[i]); }
                return CustomBic.ToArray();
            }
            return null;
        }}
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
            throw new ArgumentException("Unable to find section");
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
            List<byte> Out = new List<byte>();
            for(int i=0; i<length; i++) Out.Add(FileData[i+offset]);
            return Out.ToArray();
        }
        // NOTE: Tried to parameterize encoding with default of Civ3StringEncoding, but default must be compile-time constant
        public string GetString(int offset, int length)
        {
            byte[] StringBytes = GetBytes(offset, length);
            string Out = System.Text.Encoding.GetEncoding(Civ3StringEncoding).GetString(StringBytes);
            Regex TrimAfterNull = new Regex(@"^[^\0]*");
            Match NoNullMatch = TrimAfterNull.Match(Out);
            return NoNullMatch.Value;
        }
    }
}
