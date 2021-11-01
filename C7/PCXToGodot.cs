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

//  // Called every frame. 'delta' is the elapsed time since the previous frame.
//  public override void _Process(float delta)
//  {
//      
//  }
}
