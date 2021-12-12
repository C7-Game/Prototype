using System;
using System.IO;
using Blast;
using System.Text.RegularExpressions;
using System.Text;

namespace QueryCiv3
{
    public class Util
    {
        // Encoding code page ID; 1252 is Civ3 encoding for US language version
        //   See https://docs.microsoft.com/en-us/dotnet/api/system.text.encoding?view=net-5.0 for other values
        // NOTE: .NET 5 and later require a Nuget package and Encoder registration for these older encoding pages
        static public Encoding Civ3Encoding = Encoding.GetEncoding(1252);

        static public byte[] ReadFile(string pathName)
        {
            byte[] MyFileData = File.ReadAllBytes(pathName);
            if (MyFileData[0] == 0x00 && (MyFileData[1] == 0x04 || MyFileData[1] == 0x05 || MyFileData[1] == 0x06))
            {
                return Decompress(MyFileData);
            }
            return MyFileData;
        }

        static public byte[] Decompress(byte[] compressedBytes)
        {
            MemoryStream DecompressedStream = new MemoryStream();
            BlastDecoder Decompressor = new BlastDecoder(new MemoryStream(compressedBytes, writable: false), DecompressedStream);
            Decompressor.Decompress();
            return DecompressedStream.ToArray();
        }

        static public string GetString(byte[] bytes)
        {
            string Out = Civ3Encoding.GetString(bytes);
            Regex TrimAfterNull = new Regex(@"^[^\0]*");
            Match NoNullMatch = TrimAfterNull.Match(Out);
            return NoNullMatch.Value;
        }
    }
}
