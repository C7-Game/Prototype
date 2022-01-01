using Godot;
using System;
using ConvertCiv3Media;

public class PCXToGodot : Godot.Object
{
	private readonly static byte CIV3_TRANSPARENCY_START = 254;

	public static ImageTexture getImageTextureFromImage(Image image) {
		ImageTexture Txtr = new ImageTexture();
		Txtr.CreateFromImage(image, 0);
		return Txtr;
	}

	public static ImageTexture getImageTextureFromPCX(Pcx pcx) {
		Image ImgTxtr = ByteArrayToImage(pcx.ColorIndices, pcx.Palette, pcx.Width, pcx.Height);
		return getImageTextureFromImage(ImgTxtr);
	}

	public static ImageTexture getImageTextureFromPCX(Pcx pcx, int leftStart, int topStart, int croppedWidth, int croppedHeight) {
		Image image = getImageFromPCX(pcx, leftStart, topStart, croppedWidth, croppedHeight);
		return getImageTextureFromImage(image);
	}

	// BufferData should be as large as the largest PCX file needed to be loaded in pixels
	// For now, I will assume no PCX file is larger than 4k resolution
	public static int[] BufferData = new int[3840 * 2160];
	public static int[] ColorData = new int[256];
	public static int[] AlphaData = new int[256];

	public static void loadPalette(byte[,] palette, bool shadows = false) {
		int Red, Green, Blue;
		int Alpha = 255 << 24;

		for (int i = 0; i < 256; i++) {
			Red = palette[i, 0];
			Green = palette[i, 1];
			Blue = palette[i, 2];
			ColorData[i] = Red + (Green << 8) + (Blue << 16) + Alpha;
		}

		for (int i = CIV3_TRANSPARENCY_START; i < 256; i++) {
			ColorData[i] &= 0x00ffffff;
		}

		if (shadows) {
			for (int i = 240; i < 256; i++) {
				ColorData[i] = ((255 - i) * 16) << 24;
			}
		}
	}

	public static void loadAlphaPalette(byte[,] palette) {
		for (int i = 0; i < 256; i++) {
			// Assumption based on menuButtonsAlpha.pcx: The palette in the alpha PCX always has the same red, green, and blue values (i.e. is grayscale).
			// Examining it with breakpoints in my Java code, it appears it starts at 255, 255, 255, and goes down one at a time.  But this code
			// doesn't assume that, it only assumes the grayscale aspect.  In theory, this should work for any transparency, 0 to 255.
			AlphaData[i] = palette[i, 0] << 24;
		}
	}

	public static Image getImageFromBufferData(int width, int height) {
		Image image = new Image();
		var Data = new byte[4 * width * height];
		Buffer.BlockCopy(BufferData, 0, Data, 0, 4 * width * height);
		image.CreateFromData(width, height, false, Image.Format.Rgba8, Data);
		return image;
	}

	/**
	 * This method is for cases where we want to use components of multiple PCXs in a texture, such as for the popup background.
	 **/
	public static Image getImageFromPCX(Pcx pcx, int leftStart, int topStart, int croppedWidth, int croppedHeight) {
		loadPalette(pcx.Palette);

		int yEnd = topStart + croppedHeight;
		int xEnd = leftStart + croppedWidth;
		int Index;
		int DataIndex = 0;

		for (int y = topStart; y < yEnd; y++) {
			Index = y * pcx.Width + leftStart;
			for (int x = leftStart; x < xEnd; x++) {
				BufferData[DataIndex++] = ColorData[pcx.ColorIndices[Index++]];
			}
		}

		return getImageFromBufferData(croppedWidth, croppedHeight);
	}

	private static Image ByteArrayToImage(byte[] colorIndices, byte[,] palette, int width, int height, int[] transparent = null, bool shadows = false) {
		loadPalette(palette, shadows);

		int length = width * height;
		for (int i = 0; i < length; i++) {
			BufferData[i] = ColorData[colorIndices[i]];
		}

		return getImageFromBufferData(width, height);
	}

	public static ImageTexture getImageFromPCXWithAlphaBlend(Pcx imagePcx, Pcx alphaPcx) {
		return getImageFromPCXWithAlphaBlend(imagePcx, alphaPcx, 0, 0, imagePcx.Width, imagePcx.Height);
	}

	//Combines two PCXs, one used for the alpha, to produce a final output image.
	//Some files, such as Art/interface/menuButtons.pcx and Art/interface/menuButtonsAlpha.pcx, use this method.
	public static ImageTexture getImageFromPCXWithAlphaBlend(Pcx imagePcx, Pcx alphaPcx, int leftStart, int topStart, int croppedWidth, int croppedHeight, int alphaRowOffset = 0) {
		loadPalette(imagePcx.Palette);
		loadAlphaPalette(alphaPcx.Palette);

		int yEnd = topStart + croppedHeight;
		int xEnd = leftStart + croppedWidth;
		int Index;
		int AlphaIndex;
		int DataIndex = 0;

		for (int y = topStart; y < yEnd; y++) {
			Index = y * imagePcx.Width + leftStart;
			AlphaIndex = (y - alphaRowOffset) * imagePcx.Width + leftStart;
			for (int x = leftStart; x < xEnd; x++) {
				BufferData[DataIndex++] = (ColorData[imagePcx.ColorIndices[Index++]] & 0x00ffffff) | AlphaData[alphaPcx.ColorIndices[AlphaIndex++]];
			}
		}

		Image OutImage = getImageFromBufferData(croppedWidth, croppedHeight);
		return getImageTextureFromImage(OutImage);
	}
}
