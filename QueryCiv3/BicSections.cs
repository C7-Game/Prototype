using System;
using System.Collections.Generic;

namespace QueryCiv3
{
    public class BldgSection : SectionListItem
    {
        public string Name { get => Bic.GetString(Offset+64, 32); }
        // TODO: I'm sure this is a dumb name
        public string Reference { get => Bic.GetString(Offset+96, 32); }
    }
    public class CtznSection : SectionListItem {}
    public class CultSection : SectionListItem {}
    public class DiffSection : SectionListItem {}
    public class ErasSection : SectionListItem {}
    public class EspnSection : SectionListItem {}
    public class ExprSection : SectionListItem {}
    public class GoodSection : SectionListItem {}
    public class GovtSection : SectionListItem {}
    public class PrtoSection : SectionListItem {}
    public class RaceSection : SectionListItem {}
    public class TechSection : SectionListItem {}
    public class TfrmSection : SectionListItem {}
    public class TerrSection : SectionListItem {
        public int numPossibleResources { get => Bic.ReadInt32(Offset); }

        //This contains one byte for each 8 resources available on the tile.
        //Thus, if there are 0 resource, 0 bytes; 1-8 resources, 1 bytes, 9-16, 2 bytes, etc.
        public byte[] resourcesAllowedOnTerrain {
            get {
                int bytesNeededForResources = (numPossibleResources + 7) /8;
                byte[] byteBuffer = new byte[bytesNeededForResources];
                Array.Copy(RawBytes, 4, byteBuffer, 0, byteBuffer.Length);
                return byteBuffer;
            }
        }
        public int terrainNameOffset { get => Offset + 4 + resourcesAllowedOnTerrain.Length; }
        public string terrainName { get => Bic.GetString(terrainNameOffset, 32); }
        public string civilopediaEntry { get => Bic.GetString(terrainNameOffset + 32, 32); }
        public int irrigationBonus { get => Bic.ReadInt32(terrainNameOffset + 64);}
        public int miningBonus { get => Bic.ReadInt32(terrainNameOffset + 68);}
        public int roadBonus { get => Bic.ReadInt32(terrainNameOffset + 72);}
        public int defenseBonus { get => Bic.ReadInt32(terrainNameOffset + 76);}
        public int movementCost { get => Bic.ReadInt32(terrainNameOffset + 80);}
        public int food { get => Bic.ReadInt32(terrainNameOffset + 84);}
        public int shields { get => Bic.ReadInt32(terrainNameOffset + 88);}
        public int commerce { get => Bic.ReadInt32(terrainNameOffset + 92);}
        //Which worker job (TFRM) can be performed on this terrain type
        public int workerJobAllowed { get => Bic.ReadInt32(terrainNameOffset + 96);}
        //Which Terrain this Terrain becomes if affected by pollution.  -1 = not affected.  14 = Base Terrain Type (probably 12 in Vanilla/PTW)
        public int pollutionEffect { get => Bic.ReadInt32(terrainNameOffset + 100);}
        public byte allowCities { get => Bic.ReadByte(terrainNameOffset + 104);}
        public byte allowColonies { get => Bic.ReadByte(terrainNameOffset + 105);}
        public byte impassable { get => Bic.ReadByte(terrainNameOffset + 106);}
        public byte impassableByWheeled { get => Bic.ReadByte(terrainNameOffset + 107);}
        public byte allowAirfields { get => Bic.ReadByte(terrainNameOffset + 108);}
        public byte allowForts { get => Bic.ReadByte(terrainNameOffset + 109);}
        public byte allowOutposts { get => Bic.ReadByte(terrainNameOffset + 110);}
        public byte allowRadarTowers { get => Bic.ReadByte(terrainNameOffset + 111);}
        public int unknownOne { get => Bic.ReadInt32(terrainNameOffset + 112);}
        public byte landmarkEnabled { get => Bic.ReadByte(terrainNameOffset + 116);}
        public int landmarkFood { get => Bic.ReadInt32(terrainNameOffset + 117);}
        public int landmarkShields { get => Bic.ReadInt32(terrainNameOffset + 121);}
        public int landmarkCommerce { get => Bic.ReadInt32(terrainNameOffset + 125);}
        public int landmarkIrrigationBonus { get => Bic.ReadInt32(terrainNameOffset + 129);}
        public int landmarkMiningBonus { get => Bic.ReadInt32(terrainNameOffset + 133);}
        public int landmarkRoadBonus { get => Bic.ReadInt32(terrainNameOffset + 137);}
        public int landmarkMovementCost { get => Bic.ReadInt32(terrainNameOffset + 141);}
        public int landmarkDefensiveBonus { get => Bic.ReadInt32(terrainNameOffset + 145);}
        public string landmarkName { get => Bic.GetString(terrainNameOffset + 149, 32);}
        public string landmarkCivilopediaEntry { get => Bic.GetString(terrainNameOffset + 181, 32);}
        public int unknownTwo { get => Bic.ReadInt32(terrainNameOffset + 213);}
        public int terrainFlags { get => Bic.ReadInt32(terrainNameOffset + 217);}
        public int diseaseStrength { get => Bic.ReadInt32(terrainNameOffset + 221);}

        public override string ToString() {
            return terrainName + " (" + food + "/" + shields + "/" + commerce + "); LM name = " + landmarkName;
        }
    }
    public class WsizSection : SectionListItem {}
    public class FlavSection : SectionListItem {}

}