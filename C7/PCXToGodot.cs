using Godot;
using System;
using ConvertCiv3Media;

public class PCXToGodot : Godot.Object
{
	// Declare member variables here. Examples:
	// private int a = 2;
	// private string b = "text";
	
	public static ImageTexture getImageTextureFromPCX(Pcx pcx) {
		Image ImgTxtr = ByteArrayToImage(pcx.Image, pcx.Palette, pcx.Width, pcx.Height);
		ImageTexture Txtr = new ImageTexture();
		Txtr.CreateFromImage(ImgTxtr, 0);
		return Txtr;
	}
	
	private static Image ByteArrayToImage(byte[] ba, byte[,] palette, int width, int height, int[] transparent = null, bool shadows = false) {
		Image OutImage = new Image();
		OutImage.Create(width, height, false, Image.Format.Rgba8);
		OutImage.Lock();
		for (int i = 0; i < width * height; i++)
		{
			if (shadows && ba[i] > 239) {
				// using black and transparency
				OutImage.SetPixel(i % width, i / width, Color.Color8(0,0,0, (byte)((255 -ba[i]) * 16)));
				// using the palette color but adding transparency
				// OutImage.SetPixel(i % width, i / width, Color.Color8(palette[ba[i],0], palette[ba[i],1], palette[ba[i],2], (byte)((255 -ba[i]) * 16)));
			} else {
				OutImage.SetPixel(i % width, i / width, Color.Color8(palette[ba[i],0], palette[ba[i],1], palette[ba[i],2], ba[i] == 255 ? (byte)0 : (byte)255));
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
