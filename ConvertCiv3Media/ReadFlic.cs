using System;
using System.IO;
using Serilog;

namespace ConvertCiv3Media
{
	// Under construction
	// Not intended to be a generalized/universal Flic reader
	// Implementing from description at https://www.drdobbs.com/windows/the-flic-file-format/184408954
	public class Flic {
		private static ILogger log = Log.ForContext<Flic>();
		// Images is an array of animations,images, each of which is a byte array of palette indexes
		public byte[,][] Images;
		// All animations/images have same palette, height, and width
		// Palette is 256 colors in red, green, blue order
		public byte[,] Palette = new byte[256,3];
		public int Width = 0;
		public int Height = 0;
		public int NumAnimations = 0;
		public int FramesPerAnimation = 0;

		private string path;

		// constructors
		public Flic(){}
		public Flic(string path) {
			this.path = path;
			this.Load(path);
		}

		public void Load(string path) {
			byte[] FlicBytes = File.ReadAllBytes(path);

			int FileFormat = BitConverter.ToUInt16(FlicBytes, 4);
			// Should be 0xAF12
			if (FileFormat != 0xaf12) {
				throw new ApplicationException("Flic version # " + FileFormat.ToString("X4") + "does not match 0xaf12");
			}

			// TODO: this may not be right for Civ3 FLCs
			int NumFrames = BitConverter.ToUInt16(FlicBytes, 6);
			this.Width = BitConverter.ToUInt16(FlicBytes, 8);
			this.Height = BitConverter.ToUInt16(FlicBytes, 10);
			int ImageLength = this.Width * this.Height;

			// Civ3-specific values
			this.NumAnimations = BitConverter.ToUInt16(FlicBytes, 0x60);
			// but every animation has a ring frame, so there are this many frames plus one for each
			this.FramesPerAnimation = BitConverter.ToUInt16(FlicBytes, 0x62);
			// Leaderheads don't have the above values, so revert to act like a regular Flic
			// TODO: See if this affects my ring-frame skip
			if (NumAnimations == 0) {
				this.NumAnimations = 1;
				this.FramesPerAnimation = NumFrames;
			}

			// Initialize image frames
			this.Images = new byte[NumAnimations, this.FramesPerAnimation][];
			for (int i = 0; i < this.NumAnimations; i++) {
				for (int j = 0; j < this.FramesPerAnimation; j++) {
					this.Images[i,j] = new byte[this.Width * this.Height];
				}
			}

			// technically should be UInt32 I think
			// frame 1 chunk offset
			int Offset = BitConverter.ToInt32(FlicBytes, 80);

			// Animations loop
			for (int anim = 0; anim < NumAnimations; anim++) {
				// Flic frames loop
				for (int f = 0; f < this.FramesPerAnimation; f++) {
					// Frame chunk headers should be 0xF1Fa; prefix chunk header is 0xF100
					// TODO: add exceptions if the headers don't match?
					int ChunkLength = BitConverter.ToInt32(FlicBytes, Offset);
					int Chunktype = BitConverter.ToUInt16(FlicBytes, Offset + 4);
					int NumSubChunks = BitConverter.ToUInt16(FlicBytes, Offset + 6);

					// Chunk loop; I may be mixing up chunks, frames and subchunks in var names
					for (int i = 0, SubOffset = Offset + 16; i < NumSubChunks; i++) {
						int SubChunkLength = BitConverter.ToInt32(FlicBytes, SubOffset);
						int SubChunkType = BitConverter.ToUInt16(FlicBytes, SubOffset + 4);
						switch (SubChunkType) {
							case 4:
								// Palette chunk
								int NumPackets = BitConverter.ToUInt16(FlicBytes, SubOffset + 6);
								if (NumPackets != 1) {
									throw new ApplicationException("Unable to deal with color palette with more than one packet; NumPackets = " + NumPackets);
								}
								int SkipCount = BitConverter.GetBytes(BitConverter.ToChar(FlicBytes, SubOffset + 8))[0];
								if (SkipCount != 0) {
									throw new ApplicationException("Unable to deal with color palette with non-zero SkipCount = " + SkipCount);
								}
								int CopyCount = BitConverter.GetBytes(BitConverter.ToChar(FlicBytes, SubOffset + 9))[0];
								if (CopyCount != 0) {
									throw new ApplicationException("Unable to deal with color palette with non-zero CopyCount = " + CopyCount);
								}
								for (int p = 0; p < 256; p++) {
									this.Palette[p,0] = FlicBytes[10 + SubOffset + p * 3];
									this.Palette[p,1] = FlicBytes[10 + SubOffset + p * 3 + 1];
									this.Palette[p,2] = FlicBytes[10 + SubOffset + p * 3 + 2];
								}
								break;
							case 15:
								// run-length-encoded full frame chunk
								for (int y = 0, x = 0, head = SubOffset + 6; y < Height; y++, x = 0) {
									// first byte of row is obsolete
									head++;
									for (; x < Width;) {
										int TypeSize = (sbyte)FlicBytes[head];
										// TypeSize == 0 makes no sense, something is wrong
										if (TypeSize == 0) {
											throw new ApplicationException("TypeSize is 0");
										}
										head++;
										// If TypeSize is positive, copy TypeSize following bytes
										// If TypeSise is negative, repeat the next byte abs(TypeSize) times
										bool CopyMany = TypeSize < 0;
										for (int foo = 0; foo < Math.Abs(TypeSize); foo++) {
											this.Images[anim,f][y * this.Width + x] = FlicBytes[head];
											x++;
											if (CopyMany) {
												head++;
											}
										}
										// If we were repeating a byte, we're still pointing at it; advance head
										if (!CopyMany) {
											head++;
										}
									}
								}
								break;
							case 7:
								// TODO: figure out why frame 0 gets overwritten
								if (f == 0) {
									break;
								}
								// diff chunk
								// Copy last frame image
								Array.Copy(this.Images[anim,f-1], this.Images[anim,f], this.Images[anim,f].Length);
								int NumLines = BitConverter.ToUInt16(FlicBytes, SubOffset + 6);
								for (int Line = 0, y = 0, head = SubOffset + 8; Line < NumLines; Line++) {
									int WordsPerLine = BitConverter.ToInt16(FlicBytes, head);
									head += 2;
									// if two high bits are 1s, this is a special skip-lines word
									if ((WordsPerLine & 0xc00) == 0xc00) {
										y += Math.Abs(WordsPerLine);
										WordsPerLine = BitConverter.ToInt16(FlicBytes, head);
										head+=2;
									}
									// If two high bits are 10, this is a special word to set the last pixel for odd-length lines
									// This may not have been tested; none of my Flics change the last pixel
									if ((WordsPerLine & 0x800) == 0x800) {
										this.Images[anim,f][this.Width * (y + 1) - 1] = (byte)(WordsPerLine & 0xff);
										WordsPerLine = BitConverter.ToInt16(FlicBytes, head);
										head+=2;
									}
									// We're out of special words; if this word has either high bit set, throw exception
									if ((WordsPerLine & 0xc00) != 0) {
										throw new ApplicationException("WordsPerLine high bits set: " + WordsPerLine);
									}
									// I wonder if WordsPerLine should actally be PacketsPerLine
									// Loop over the packets for this line
									for (int packet = 0, x = 0; packet < WordsPerLine; packet++) {
										// least significant byte of word (first byte) is columns to skip
										x += FlicBytes[head];
										head++;
										// most significant byte of word (second byte) is number of words in the packet
										int NumWords = (sbyte)FlicBytes[head];
										bool Positive = NumWords > 0;
										head++;
										// If NumWords is positive, copy NumWords following words to image
										// If NumWords is negative, repeat the next word abs(NumWords) times
										for (int ii = 0; ii < Math.Abs(NumWords); ii++) {
											this.Images[anim,f][this.Width * y + x] = FlicBytes[head];
											this.Images[anim,f][this.Width * y + x + 1] = FlicBytes[head + 1];
											if (Positive) { head += 2; }
											x += 2;
										}
										// If NumWords was negative, we're still pointing at the repeated word, so advance head
										if (! Positive) { head += 2; }
									}
									y++;
								}
								break;
							default:
								// TODO: Have this throw an exception? Or maybe just keep skipping unkown chunks
								log.Error("Subchunk not recognized: " + SubChunkType);
								break;
						}
						SubOffset += SubChunkLength;
					}
					Offset += ChunkLength;
				}
				// skip ring frame
				int RingChunkLength = BitConverter.ToInt32(FlicBytes, Offset);
				Offset += RingChunkLength;
			}
		}

		public override string ToString()
		{
			return "FLIC " + this.path;
		}
	}
}
