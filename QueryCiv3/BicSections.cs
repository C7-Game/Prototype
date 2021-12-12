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
        //TODO: This is probably not how Puppeteer intended us to fetch the data.
        public int numPossibleResources {
            get => BitConverter.ToInt32(RawBytes, 0);
        }
        //This contains one byte for each 8 resources available on the tile.
        //Thus, if there are 0 resource, 0 bytes; 1-8 resources, 1 bytes, 9-16, 2 bytes, etc.
        public byte[] possibleResources {
            get {
                int possibleResourceByteCount = (numPossibleResources + 7) /8;
                byte[] possibleResources = new byte[possibleResourceByteCount];
                Array.Copy(RawBytes, 4, possibleResources, 0, possibleResources.Length);
                return possibleResources;
            }
        }
        public int terrainNameOffset {
            get {
                return 4 + possibleResources.Length;
            }
        }
        public string terrainName {
            get {
                byte[] terrainNameBytes = new byte[32];
                Array.Copy(RawBytes, terrainNameOffset, terrainNameBytes, 0, terrainNameBytes.Length);
                string terrainName = System.Text.Encoding.GetEncoding("Windows-1252").GetString(terrainNameBytes);
                return terrainName;
            }
        }

        public override string ToString() {
            return "Terrain " + terrainName + " with " + numPossibleResources + " possible resources";
        }
    }
    public class WsizSection : SectionListItem {}
    public class FlavSection : SectionListItem {}

}