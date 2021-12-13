using System;
using System.Runtime.InteropServices;

namespace QueryCiv3.Biq
{
    [StructLayout(LayoutKind.Sequential, Pack=1)]
    public unsafe struct DIFF
    {
        public int Length;

        private fixed byte Text[64];
        public string DifficultyLevelName {
            get {
                var Entry = new byte[64];
                fixed (byte* p1 = Text, p2 = Entry) {
                    Buffer.MemoryCopy(p1, p2, 64, 64);
                }
                return Util.GetString(Entry);
            }
        }

        public int NumberOfCitizensBornContent;
        public int MaxGovernmentTransitionTime;
        public int NumberOfAIDefensiveStartingUnits;
        public int NumberOfAIOffensiveStartingUnits;
        public int ExtraStartUnit1;
        public int ExtraStartUnit2;
        public int AdditionalFreeSupport;
        public int BonusForEachCity;
        public int AttackBonusAgainstBarbarians;
        public int CostFactor;
        public int PercentageOfOptimalCities;
        public int AIToAITradeRate;
        public int CorruptionPercentage;
        public int MilitaryLaw;
    }
}
