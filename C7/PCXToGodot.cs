using Godot;
using System;
using ConvertCiv3Media;

public class PCXToGodot : Godot.Object
{
	private readonly static byte LAST_INDEX = 255;
	
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
				byte alpha = colorIndices[i] == LAST_INDEX ? (byte)0 : (byte)255;
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
	public static ImageTexture getImageFromPCXWithAlphaBlend(Pcx imagePcx, Pcx alphaPcx, int leftStart, int topStart, int croppedWidth, int croppedHeight) {
		Image OutImage = new Image();
		OutImage.Create(imagePcx.Width, imagePcx.Height, false, Image.Format.Rgba8);
		OutImage.Lock();
		GD.Print(string.Format("imagePcx dimensions: {0}, {1}", imagePcx.Width, imagePcx.Height));
		GD.Print(string.Format("alphaPcx dimensions: {0}, {1}", alphaPcx.Width, alphaPcx.Height));
		GD.Print(string.Format("Alpha palette size: {0}", alphaPcx.Palette.Length));
		for (int i = 0; i < imagePcx.Width * imagePcx.Height; i++)
		{
			byte red = imagePcx.Palette[imagePcx.ColorIndices[i], 0];
			byte green = imagePcx.Palette[imagePcx.ColorIndices[i], 1];
			byte blue = imagePcx.Palette[imagePcx.ColorIndices[i], 2];
			//Assumption based on menuButtonsAlpha.pcx: The palette in the alpha PCX always has the same red, green, and blue values (i.e. is grayscale).
			//Examining it with breakpoints in my Java code, it appears it starts at 255, 255, 255, and goes down one at a time.  But this code
			//doesn't assume that, it only assumes the grayscale aspect.  In theory, this should work for any transparency, 0 to 255.
			byte alpha = 255;
			//TODO: Of course, the alpha image may be smaller than the non-alpha one, with implicit repeating across the non-alpha.
			//Going to have to figure that one out.  For now, make sure it doesn't blow up.
			if (i < alphaPcx.ColorIndices.Length) {
				alpha = alphaPcx.Palette[alphaPcx.ColorIndices[i], 0];
			}
			OutImage.SetPixel(i % imagePcx.Width, i / imagePcx.Width, Color.Color8(red, green, blue, alpha));
		}
		OutImage.Unlock();

		Image CroppedImage = OutImage.GetRect(new Rect2(leftStart, topStart, croppedWidth, croppedHeight));
		ImageTexture Txtr = new ImageTexture();
		Txtr.CreateFromImage(CroppedImage, 0);
		return Txtr;


	}
}
