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

        public override string ToString() {
            return "Terrain " + terrainName + " with " + numPossibleResources + " possible resources";
        }
    }
    public class WsizSection : SectionListItem {}
    public class FlavSection : SectionListItem {}

}