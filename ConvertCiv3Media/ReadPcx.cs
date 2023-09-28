using System;
using System.IO;

namespace ConvertCiv3Media
{
	public class Pcx {

		public byte[,] Palette = new byte[256, 3];
		public byte[] ColorIndices;
		public int Width = 0;
		public int Height = 0;

		// constructors
		public Pcx(){}
		public Pcx(string path) {
			this.Load(path);
		}

		public byte ColorIndexAt(int x, int y)
		{
			int pixel = y * Width + x;
			return ColorIndices[pixel];
		}

		// not a generalized pcx reader
		// assumes 8-bit image with 256-color 8-bit rgb palette
		public void Load(string path) {
			byte[] PcxBytes = File.ReadAllBytes(path);

			// Read info from PCX header
			int LeftMargin = BitConverter.ToInt16(PcxBytes, 4);
			int TopMargin = BitConverter.ToInt16(PcxBytes, 6);
			int RightMargin = BitConverter.ToInt16(PcxBytes, 8);
			int BottomMargin = BitConverter.ToInt16(PcxBytes, 10);
			// assuming 1 color plane
			// this is always even, so last byte may be junk if image width is odd
			int BytesPerLine = BitConverter.ToInt16(PcxBytes, 0x42);

			this.Width = RightMargin - LeftMargin + 1;
			this.Height = BottomMargin - TopMargin + 1;
			// Palette is 256*3 bytes at end of file
			int PaletteOffset = PcxBytes.Length - 768;

			// Populate color palette
			Buffer.BlockCopy(PcxBytes, PaletteOffset, Palette, 0, 768);

			ColorIndices = new byte[Width * Height];

			// Encoding always have even number of bytes per line; if image width is odd, there is a junk byte in every row
			bool JunkByte = BytesPerLine > Width;

			// Loop to decode run-length-encoded image data which begins at file offset 0x80
			for (int ImgIdx = 0, PcxIdx = 0x80, RunLen = 0, LineIdx = 0; ImgIdx < Width * Height; ) {
				// if two most significant bits are 11
				if ((PcxBytes[PcxIdx] & 0xc0) == 0xc0) {
					// then it & 0x3f is the run length of the following byte
					RunLen = PcxBytes[PcxIdx] & 0x3f;
					PcxIdx++;
					// Repeat the pixel in the image RunLen times
					for (int j = 0; j < RunLen; j++) {
						// Add pixel copy if it's not a junk byte
						if (!(JunkByte && LineIdx % BytesPerLine == BytesPerLine - 1)) {
							ColorIndices[ImgIdx] = PcxBytes[PcxIdx];
							ImgIdx++;
						}
						LineIdx++;
					}
					PcxIdx++;
				} else {
					// Add as literal pixel if it's not a junk byte
					if (!(JunkByte && LineIdx % BytesPerLine == BytesPerLine - 1)) {
						ColorIndices[ImgIdx] = PcxBytes[PcxIdx];
						ImgIdx++;
					}
					PcxIdx++;
					LineIdx++;
				}
			}
		}
	}
}
