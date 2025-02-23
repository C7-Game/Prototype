using Godot;
using System;
using ConvertCiv3Media;
using System.Collections.Generic;

public partial class PCXToGodot : GodotObject {
	// The set of color indexes considered transparent when loading a Civ3 PCX
	private static readonly HashSet<int> transparentColorIndexes = new() { 1, 254, 255 };

	public static ImageTexture getImageTextureFromPCX(Pcx pcx) {
		Image ImgTxtr = ByteArrayToImage(pcx.ColorIndices, pcx.Palette, pcx.Width, pcx.Height);
		return getImageTextureFromImage(ImgTxtr);
	}

	public static ImageTexture getImageTextureFromPCX(Pcx pcx, int leftStart, int topStart, int croppedWidth, int croppedHeight, bool shadows = true) {
		Image image = getImageFromPCX(pcx, leftStart, topStart, croppedWidth, croppedHeight, shadows);
		return getImageTextureFromImage(image);
	}

	/**
	 * This method is for cases where we want to use components of multiple PCXs in a texture, such as for the popup background.
	 **/
	public static Image getImageFromPCX(Pcx pcx, int leftStart, int topStart, int croppedWidth, int croppedHeight, bool shadows = true) {
		int[] ColorData = loadPalette(pcx.Palette, shadows);
		int[] BufferData = new int[croppedWidth * croppedHeight];

		int DataIndex = 0;

		for (int y = topStart; y < topStart + croppedHeight; y++) {
			for (int x = leftStart; x < leftStart + croppedWidth; x++) {
				BufferData[DataIndex] = ColorData[pcx.ColorIndexAt(x, y)];
				DataIndex++;
			}
		}

		return getImageFromBufferData(croppedWidth, croppedHeight, BufferData);
	}

	public static ImageTexture getPureAlphaFromPCX(Pcx alphaPcx) {
		int[] bufferData = new int[alphaPcx.Width * alphaPcx.Height];
		int[] alphaData = new int[256];
		for (int i = 0; i < 256; i++) {
			alphaData[i] = alphaPcx.Palette[i, 0];
		}
		int dataIndex = 0;
		for (int y = 0; y < alphaPcx.Height; y++) {
			for (int x = 0; x < alphaPcx.Width; x++, dataIndex++) {
				int index = alphaPcx.ColorIndexAt(x, y);
				if (transparentColorIndexes.Contains(index)) {
					bufferData[dataIndex] = 0;
				} else {
					bufferData[dataIndex] = alphaData[index] << 24;
				}
			}
		}

		Image outImage = getImageFromBufferData(alphaPcx.Width, alphaPcx.Height, bufferData);
		return getImageTextureFromImage(outImage);
	}

	public static ImageTexture getImageFromPCXWithAlphaBlend(Pcx imagePcx, Pcx alphaPcx) {
		return getImageFromPCXWithAlphaBlend(imagePcx, alphaPcx, 0, 0, imagePcx.Width, imagePcx.Height);
	}

	//Combines two PCXs, one used for the alpha, to produce a final output image.
	//Some files, such as Art/interface/menuButtons.pcx and Art/interface/menuButtonsAlpha.pcx, use this method.
	public static ImageTexture getImageFromPCXWithAlphaBlend(Pcx imagePcx, Pcx alphaPcx, int leftStart, int topStart, int croppedWidth, int croppedHeight, int alphaRowOffset = 0) {
		int[] ColorData = loadPalette(imagePcx.Palette, false);
		int[] AlphaData = loadAlphaPalette(alphaPcx.Palette, ColorData);
		int[] BufferData = new int[croppedWidth * croppedHeight];

		int AlphaIndex;
		int DataIndex = 0;

		for (int y = topStart; y < topStart + croppedHeight; y++) {
			AlphaIndex = (y - alphaRowOffset) * imagePcx.Width + leftStart;
			for (int x = leftStart; x < leftStart + croppedWidth; x++) {
				BufferData[DataIndex] = ColorData[imagePcx.ColorIndexAt(x, y)] | AlphaData[alphaPcx.ColorIndices[AlphaIndex]];
				DataIndex++;
				AlphaIndex++;
			}
		}

		Image OutImage = getImageFromBufferData(croppedWidth, croppedHeight, BufferData);
		return getImageTextureFromImage(OutImage);
	}

	public static Image ByteArrayToImage(byte[] colorIndices, byte[,] palette, int width, int height, int[] transparent = null, bool shadows = false) {
		int[] ColorData = loadPalette(palette, shadows);
		int[] BufferData = new int[width * height];

		for (int i = 0; i < width * height; i++) {
			BufferData[i] = ColorData[colorIndices[i]];
		}

		return getImageFromBufferData(width, height, BufferData);
	}

	// ByteArrayWithTintToImage is used to load create images from flic frames
	// that contain a tinted layer such as unit animations, where the unit's
	// clothing is tinted by their civ color.
	public static (Image, Image) ByteArrayWithTintToImage(byte[] colorIndices, byte[,] palette, int width, int height, int[] transparent = null, bool shadows = false) {
		int[] colorData = loadPalette(palette, shadows);
		int[] baseLayer = new int[width * height];
		int[] tintLayer = new int[width * height];

		Pcx whitePcx = Util.LoadPCX("Art/Units/Palettes/ntp00.pcx");
		int[] whiteColorData = loadPalette(whitePcx.Palette, true);

		for (int i = 0; i < width * height; i++) {
			int index = colorIndices[i];
			bool tinted = index < 16 || (index < 64 && index % 2 == 0);
			bool shadow = index >= 224 && index <= 239;
			if (tinted) {
				tintLayer[i] = whiteColorData[index];
				baseLayer[i] = 0; // transparent
			} else if (shadow) {
				// shadow belongs to the base texture
				baseLayer[i] = ((int)new Color(1.0f, 1.0f, 1.0f, (float)index - 224f / 239f - 224f).ToArgb32());
				tintLayer[i] = 0; // transparent
			} else {
				baseLayer[i] = colorData[index];
				tintLayer[i] = 0; // transparent
			}
		}
		return (getImageFromBufferData(width, height, baseLayer), getImageFromBufferData(width, height, tintLayer));
	}

	// Utility for loading a civilization color from an ntp file
	public static Color GetColorFromPCX(Pcx pcx) {
		int paletteLookupIdx = pcx.ColorIndexAt(0, 0);

		return Color.Color8(
			pcx.Palette[paletteLookupIdx, 0],
			pcx.Palette[paletteLookupIdx, 1],
			pcx.Palette[paletteLookupIdx, 2]
		);
	}

	private static Image getImageFromBufferData(int width, int height, int[] bufferData) {
		byte[] Data = new byte[4 * width * height];
		Buffer.BlockCopy(bufferData, 0, Data, 0, 4 * width * height);
		Image image = Image.CreateFromData(width, height, false, Image.Format.Rgba8, Data);
		return image;
	}

	private static ImageTexture getImageTextureFromImage(Image image) {
		return ImageTexture.CreateFromImage(image);
	}

	private static int[] loadPalette(byte[,] palette, bool shadows) {
		int Red, Green, Blue;
		int[] ColorData = new int[256];

		for (int i = 0; i < 256; i++) {
			Red = palette[i, 0];
			Green = palette[i, 1] << 8;
			Blue = palette[i, 2] << 16;

			int Alpha = transparentColorIndexes.Contains(i) ? 0 : 255 << 24;

			ColorData[i] = Red + Green + Blue + Alpha;
		}

		if (shadows) {
			for (int i = 240; i < 256; i++) {
				ColorData[i] = ((255 - i) * 16) << 24;
			}
		}

		return ColorData;
	}

	private static int[] loadAlphaPalette(byte[,] palette, int[] ColorData) {
		int[] AlphaData = new int[256];

		for (int i = 0; i < 256; i++) {
			// Assumption based on menuButtonsAlpha.pcx: The palette in the alpha PCX always has the same red, green, and blue values (i.e. is grayscale).
			// Examining it with breakpoints in my Java code, it appears it starts at 255, 255, 255, and goes down one at a time.  But this code
			// doesn't assume that, it only assumes the grayscale aspect.  In theory, this should work for any transparency, 0 to 255.
			AlphaData[i] = palette[i, 0] << 24;
			ColorData[i] = ColorData[i] &= 0x00ffffff;
		}

		return AlphaData;
	}
}
