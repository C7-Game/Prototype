using Godot;
using System;
using ConvertCiv3Media;

public class PCXToGodot : Godot.Object
{
	private readonly static byte CIV3_TRANSPARENCY_START = 254;
	
	public static ImageTexture getImageTextureFromPCX(Pcx pcx) {
		Image ImgTxtr = ByteArrayToImage(pcx.ColorIndices, pcx.Palette, pcx.Width, pcx.Height);
		ImageTexture Txtr = new ImageTexture();
		Txtr.CreateFromImage(ImgTxtr, 0);
		return Txtr;
	}

	public static ImageTexture getImageTextureFromPCX(Pcx pcx, int leftStart, int topStart, int width, int height) {
		Image Image = ByteArrayToImage(pcx.ColorIndices, pcx.Palette, pcx.Width, pcx.Height);
		Image = Image.GetRect(new Rect2(leftStart, topStart, width, height));
		ImageTexture Txtr = new ImageTexture();
		Txtr.CreateFromImage(Image, 0);
		return Txtr;
	}

	/**
	 * This method is for cases where we want to use components of multiple PCXs in a texture, such as for the popup background.
	 **/
	public static Image getImageFromPCX(Pcx pcx, int leftStart, int topStart, int croppedWidth, int croppedHeight) {
		Image image = new Image();
		image.Create(croppedWidth, croppedHeight, false, Image.Format.Rgba8);
		image.Lock();
		for (int y = topStart; y < topStart + croppedHeight; y++)
		{
			for (int x = leftStart; x < leftStart + croppedWidth; x++)
			{
				byte red = pcx.Palette[pcx.ColorIndexAt(x, y), 0];
				byte green = pcx.Palette[pcx.ColorIndexAt(x, y), 1];
				byte blue = pcx.Palette[pcx.ColorIndexAt(x, y), 2];
				image.SetPixel(x - leftStart, y - topStart, Color.Color8(red, green, blue));
			}
		}
		image.Unlock();
		return image;
	}
	
	private static Image ByteArrayToImage(byte[] colorIndices, byte[,] palette, int width, int height, int[] transparent = null, bool shadows = false) {
		Image OutImage = new Image();
		OutImage.Create(width, height, false, Image.Format.Rgba8);
		OutImage.Lock();
		for (int i = 0; i < width * height; i++)
		{
			if (shadows && colorIndices[i] > 239) {
				// using black and transparency
				OutImage.SetPixel(i % width, i / width, Color.Color8(0,0,0, (byte)((255 -colorIndices[i]) * 16)));
				// using the palette color but adding transparency
				// OutImage.SetPixel(i % width, i / width, Color.Color8(palette[ba[i],0], palette[ba[i],1], palette[ba[i],2], (byte)((255 -ba[i]) * 16)));
			} else {
				byte red = palette[colorIndices[i], 0];
				byte green = palette[colorIndices[i], 1];
				byte blue = palette[colorIndices[i], 2];
				byte alpha = colorIndices[i] >= CIV3_TRANSPARENCY_START ? (byte)0 : (byte)255;
				OutImage.SetPixel(i % width, i / width, Color.Color8(red, green, blue, alpha));
			}
		}
		OutImage.Unlock();

		return OutImage;
	}

	public static ImageTexture getImageFromPCXWithAlphaBlend(Pcx imagePcx, Pcx alphaPcx) {
		return getImageFromPCXWithAlphaBlend(imagePcx, alphaPcx, 0, 0, imagePcx.Width, imagePcx.Height);
	}

	//Combines two PCXs, one used for the alpha, to produce a final output image.
	//Some files, such as Art/interface/menuButtons.pcx and Art/interface/menuButtonsAlpha.pcx, use this method.
	public static ImageTexture getImageFromPCXWithAlphaBlend(Pcx imagePcx, Pcx alphaPcx, int leftStart, int topStart, int croppedWidth, int croppedHeight, int alphaRowOffset = 0) {
		Image OutImage = new Image();
		OutImage.Create(croppedWidth, croppedHeight, false, Image.Format.Rgba8);
		OutImage.Lock();
		for (int y = topStart; y < topStart + croppedHeight; y++)
		{
			for (int x = leftStart; x < leftStart + croppedWidth; x++)
			{
				int currentPixel = y * imagePcx.Width + x;
				byte red = imagePcx.Palette[imagePcx.ColorIndexAt(x, y), 0];
				byte green = imagePcx.Palette[imagePcx.ColorIndexAt(x, y), 1];
				byte blue = imagePcx.Palette[imagePcx.ColorIndexAt(x, y), 2];
				//Assumption based on menuButtonsAlpha.pcx: The palette in the alpha PCX always has the same red, green, and blue values (i.e. is grayscale).
				//Examining it with breakpoints in my Java code, it appears it starts at 255, 255, 255, and goes down one at a time.  But this code
				//doesn't assume that, it only assumes the grayscale aspect.  In theory, this should work for any transparency, 0 to 255.
				byte alpha = alphaPcx.Palette[alphaPcx.ColorIndexAt(x, y - alphaRowOffset), 0];
				OutImage.SetPixel(x - leftStart, y - topStart, Color.Color8(red, green, blue, alpha));
			}
		}
		OutImage.Unlock();

		ImageTexture Txtr = new ImageTexture();
		Txtr.CreateFromImage(OutImage, 0);
		return Txtr;


	}
}
