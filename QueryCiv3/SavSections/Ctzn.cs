using System;
using System.Runtime.InteropServices;

namespace QueryCiv3.Sav
{
    [StructLayout(LayoutKind.Sequential, Pack=1)]
    public unsafe struct CTZN
    {
        private fixed byte HeaderText[4];
        public int Length;
        private fixed byte UnknownBuffer[1];
        public byte TileWorked;

        private fixed byte Text[262];
        public string Name { get => Util.GetString(ref this, 10, 262); }

        public int Type; // 0: Happy, 1: Content, 2: Sad, 3: Resistor, 4: Specialist
        public int Sex; // 0: Male, 1: Female
        public int BirthDate;
        public int CityID;
        public int ID;
        public int SpecialistType;
        public int Nationality;
        public int AffectedBy;
        public int AffectedSince;
    }
}
