using System;
using System.Runtime.InteropServices;

namespace QueryCiv3.Biq
{
    [StructLayout(LayoutKind.Sequential, Pack=1)]
    public unsafe struct UNIT
    {
        public int Length;

        private fixed byte Text[32];
        public string Name { get => Util.GetString(ref this, 4, 32); }

        public int OwnerType; // 0: None, 1: Barbarian, 2: Civ, 3: Player
        public int ExperienceLevel;
        public int Owner;
        public int Unit;
        public int AIStrategy;
        public int X;
        public int Y;

        private fixed byte Text2[57];
        public string PTWName { get => Util.GetString(ref this, 64, 57); }

        public int UseCivilizationKing;
    }
}
