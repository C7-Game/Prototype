using System;
using System.IO;
using Blast;

namespace QueryCiv3
{
    public class Util
    {
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
    }
}