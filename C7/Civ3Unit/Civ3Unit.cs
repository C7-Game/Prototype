using Godot;
using ConvertCiv3Media;
using System;

public partial class Civ3Unit : Civ3UnitSprite
{
	public MovingSprite AS;
	public SpriteFrames SF;
	// constructor to copy existing unit
	public Civ3Unit(Civ3Unit civ3Unit) : base(civ3Unit) {
		this.AS = (MovingSprite)civ3Unit.AS.Duplicate();
		this.SF = (SpriteFrames)civ3Unit.SF.Duplicate();
		this.AS.SpriteFrames = this.SF;
	}

	public Civ3Unit(string path, byte unitColor = 0) : base(path, unitColor) {
		this.AS = new MovingSprite();
		this.AS.Position = new Vector2(128 * 5, 64 * 3 - 12);
		// temporarily making it bigger
		// this.AS.Scale = new Vector2(2, 2);
		this.SF = new SpriteFrames();
		this.AS.SpriteFrames = SF;
		// TODO: Loop through animations and create sprites
		foreach (UnitAction actn in Enum.GetValues(typeof(UnitAction))) {
			// Ensuring there is image data for this action
			if (Animations[(int)actn] != null) {
				foreach (Direction dir in Enum.GetValues(typeof(Direction))) {
					string ActionAndDirection = String.Format("{0}-{1}", actn.ToString(), dir.ToString());
					SF.AddAnimation(ActionAndDirection); // TODO: make sure this is the same
					SF.SetAnimationSpeed(ActionAndDirection, 10);

					for (int i = 0; i < Animations[(int)actn].Images.GetLength(1); i++) {
						Image ImgTxtr = Civ3Unit.ByteArrayToImage(
							Animations[(int)actn].Images[(int)dir,i],
							Animations[(int)actn].Palette,
							Animations[(int)actn].Width,
							Animations[(int)actn].Height,
							shadows: true
						);
						ImageTexture Txtr = ImageTexture.CreateFromImage(ImgTxtr);
						SF.AddFrame(ActionAndDirection, Txtr);
					}

				}
			}
		}
	}
	public override void Animation(UnitAction action, Direction direction) {
		string actnName = action.ToString() + "-" + direction.ToString();
		if (!SF.HasAnimation(actnName)) {
			throw new ApplicationException(String.Format("Animation does not exist for {0} action and {1} direction", action.ToString(), direction.ToString()));
		}
		AS.Play(actnName);
	}
	public override void Move(Direction direction, float speed = (float)0.6) {
		switch (direction)
		{
			case Direction.SW:
				AS.Velocity = new Vector2(-2, 1).Normalized() * speed;
				break;
			case Direction.S:
				AS.Velocity = new Vector2(0, 1) * speed;
				break;
			case Direction.SE:
				AS.Velocity = new Vector2(2, 1).Normalized() * speed;
				break;
			case Direction.E:
				AS.Velocity = new Vector2(1, 0) * speed;
				break;
			case Direction.NE:
				AS.Velocity = new Vector2(2, -1).Normalized() * speed;
				break;
			case Direction.N:
				AS.Velocity = new Vector2(0, -1) * speed;
				break;
			case Direction.NW:
				AS.Velocity = new Vector2(-2, -1).Normalized() * speed;
				break;
			case Direction.W:
				AS.Velocity = new Vector2(-1, 0) * speed;
				break;
		}
	}

	// TODO: This is mostly duplicated in/from PCXToGodot.cs, but special indexes
	//   handled differently. Probably needs combining and refactoring
	public static Image ByteArrayToImage(byte[] colorIndices, byte[,] palette, int width, int height, int[] transparent = null, bool shadows = false) {
		Image OutImage = Image.Create(width, height, false, Image.Format.Rgba8);
		byte[] bmpBuffer = getBmpBuffer(colorIndices, palette, width, height, transparent, shadows);
		OutImage.LoadBmpFromBuffer(bmpBuffer);

		return OutImage;
	}

	public static unsafe byte[] getBmpBuffer(byte[] colorIndices, byte[,] palette, int width, int height, int[] transparent = null, bool shadows = false) {

		int bmpSize = width * height * 4 + 54;  //54 = Windows 3 BMP header size.
		byte[] bmpBuffer = new byte[bmpSize];
		fixed (byte* fPtr = bmpBuffer) {
			//BMP header.  This is a mix of byte, short, and int.  Probably a cleaner way to do this, but my C is rusty.
			byte* bPtr = fPtr;
			int* iPtr;
			short* sPtr;
			bPtr[0] = 66;    //B
			bPtr[1] = 77;    //M
			bPtr+=2;
			iPtr = (int*)bPtr;
			iPtr[0] = bmpSize;  //size of BMP file in bytes
			iPtr[1] = 0;    //reserved, two shorts
			iPtr[2] = 54;   //offset of image data from start of file
			iPtr[3] = 40;   //size of Windows 3 BMP header
			iPtr[4] = width;
			iPtr[5] = height;
			bPtr+=24;   //6 * 4
			sPtr = (short*)bPtr;
			sPtr[0] = 1;    //num color planes
			sPtr[1] = 32;   //bit depth.  we want 32-bit with alpha
			bPtr+=4;
			iPtr = (int*)bPtr;
			iPtr[0] = 0;    //compression - none.  Godot doesn't support compression
			iPtr[1] = bmpSize - 54; //size, without headers
			iPtr[2] = 0;    //horizontal resolution.  ignored.
			iPtr[3] = 0;    //vertical resolution.  ignored.
			iPtr[4] = 0;    //num colors in palette.  not relevant for 32-bit
			iPtr[5] = 0;    //num important colors in palette.  not relevant.
			bPtr+=24;   //6 * 4
			iPtr = (int*)bPtr;
			//Ready for image data

			int c = 0;
			//The BMP data is stored bottom-to-top, whereas our data is top-to-bottom
			//Thus we'll use c to calculate the index for each row
			for (int y = height - 1; y > -1; y--)
			{
				c = width * y;
				for (int x = 0; x < width; x++)
				{
					if (shadows && colorIndices[c] > 239) {
						// using black and transparency
						iPtr[0] = ((255 - colorIndices[c]) << 4) << 24;
						// using the palette color but adding transparency
						// OutImage.SetPixel(i % width, i / width, Color.Color8(palette[ba[i],0], palette[ba[i],1], palette[ba[i],2], (byte)((255 -ba[i]) * 16)));
					} else {
						// A, R, G, B
						iPtr[0] = ((byte)255) << 24 | palette[colorIndices[c], 0] << 16 | palette[colorIndices[c], 1] << 8 | palette[colorIndices[c], 2] << 0;
					}
					c++;
					iPtr++;
				}
			}
		}
		return bmpBuffer;
	}
}
// This is just to add some movement to an AnimatedSprite2D
public partial class MovingSprite : AnimatedSprite2D {
	public Vector2 Velocity = new Vector2(0, 0);
	public override void _PhysicsProcess(double delta) {
		Position = Position + Velocity;
		if (Position.X > 1040) {
			Position = new Vector2(-30, Position.Y);
		}
		if (Position.X < -30) {
			Position = new Vector2(1040, Position.Y);
		}
		if (Position.Y > 800) {
			Position = new Vector2(Position.X, -30);
		}
		if (Position.Y < -30) {
			Position = new Vector2(Position.X, 800);
		}
	}
}

