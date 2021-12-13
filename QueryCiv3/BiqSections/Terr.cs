using System;
using System.Runtime.InteropServices;

namespace QueryCiv3.Biq
{
    [StructLayout(LayoutKind.Sequential, Pack=1)]
    public unsafe struct TERR
    {
        public int Length;
        public int NumPossibleResources;
        /*

        */

        private fixed byte Text[64];
        public string TerrainName {
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

        public int IrrigationBonus;
        public int MiningBonus;
        public int RoadBonus;
        public int DefenseBonus;
        public int MovementCost;
        public int Food;
        public int Shields;
        public int Commerce;
        // Which worker job (TFRM) can be performed on this terrain type
        public int WorkerJobAllowed;
        // Which Terrain this Terrain becomes if affected by pollution.  -1 = not affected.  14 = Base Terrain Type (probably 12 in Vanilla/PTW)
        public int PollutionEffect;
        public byte AllowCities;
        public byte AllowColonies;
        public byte Impassable;
        public byte ImpassableByWheeled;
        public byte AllowAirfields;
        public byte AllowForts;
        public byte AllowOutposts;
        public byte AllowRadarTowers;
        private fixed byte Unknown[4];
        public byte LandmarkEnabled;
        public int LandmarkFood;
        public int LandmarkShields;
        public int LandmarkCommerce;
        public int LandmarkIrrigationBonus;
        public int LandmarkMiningBonus;
        public int LandmarkRoadBonus;
        public int LandmarkMovementBonus;
        public int LandmarkDefensiveBonus;

        private fixed byte Text2[64];
        public string LandmarkName {
            get {
                var Entry = new byte[32];
                fixed (byte* p1 = Text2, p2 = Entry) {
                    Buffer.MemoryCopy(p1, p2, 32, 32);
                }
                return Util.GetString(Entry);
            }
        }
        public string LandmarkCivilopediaEntry {
            get {
                var Entry = new byte[32];
                fixed (byte* p1 = Text2, p2 = Entry) {
                    Buffer.MemoryCopy(p1 + 32, p2, 32, 32);
                }
                return Util.GetString(Entry);
            }
        }

        private fixed byte Unknown2[4];
        public int TerrainFlags;
        public int DiseaseStrength;
    }
}
