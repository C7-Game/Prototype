using Godot;
using ConvertCiv3Media;
using System;

public class Civ3Unit : Civ3UnitSprite
{
    public MovingSprite AS;
    public SpriteFrames SF;
    // constructor to copy existing unit
    public Civ3Unit(Civ3Unit civ3Unit) : base(civ3Unit) {
        this.AS = (MovingSprite)civ3Unit.AS.Duplicate();
        this.SF = (SpriteFrames)civ3Unit.SF.Duplicate();
        this.AS.Frames = this.SF;
    }
    public Civ3Unit(string path, byte unitColor = 0) : base(path, unitColor) {
        AS = new MovingSprite();
        AS.Position = new Vector2(128 * 5, 64 * 3 - 12);
        // temporarily making it bigger
        // AS.Scale = new Vector2(2, 2);
        SF = new SpriteFrames();
        AS.Frames = SF;
        // TODO: Loop through animations and create sprites
        foreach (UnitAction actn in Enum.GetValues(typeof(UnitAction))) {
            // Ensuring there is image data for this action
            if (Animations[(int)actn] != null) {
                foreach (Direction dir in Enum.GetValues(typeof(Direction))) {
                    string ActionAndDirection = String.Format("{0}-{1}", actn.ToString(), dir.ToString());
                    SF.AddAnimation(ActionAndDirection);
                    SF.SetAnimationSpeed(ActionAndDirection, 10);

                    for (int i = 0; i < Animations[(int)actn].Images.GetLength(1); i++) {
                        Image ImgTxtr = Civ3Unit.ByteArrayToImage(
                            Animations[(int)actn].Images[(int)dir,i],
                            Animations[(int)actn].Palette,
                            Animations[(int)actn].Width,
                            Animations[(int)actn].Height,
                            shadows: true
                        );
                        ImageTexture Txtr = new ImageTexture();
                        // TODO: parametrize flags parameter
                        Txtr.CreateFromImage(ImgTxtr, 7);
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
                AS.Velocity = new Vector2(-1, 1).Normalized() * speed;
                break;
            case Direction.S:
                AS.Velocity = new Vector2(0, 1) * speed;
                break;
            case Direction.SE:
                AS.Velocity = new Vector2(1, 1).Normalized() * speed;
                break;
            case Direction.E:
                AS.Velocity = new Vector2(1, 0) * speed;
                break;
            case Direction.NE:
                AS.Velocity = new Vector2(1, -1).Normalized() * speed;
                break;
            case Direction.N:
                AS.Velocity = new Vector2(0, -1) * speed;
                break;
            case Direction.NW:
                AS.Velocity = new Vector2(-1, -1).Normalized() * speed;
                break;
            case Direction.W:
                AS.Velocity = new Vector2(-1, 0) * speed;
                break;
        }
    }

    // TODO: This is mostly duplicated in/from PCXToGodot.cs, but special indexes
    //   handled differently. Probably needs combining and refactoring
    public static Image ByteArrayToImage(byte[] ba, byte[,] palette, int width, int height, int[] transparent = null, bool shadows = false) {
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
}
// This is just to add some movement to an AnimatedSprite
public class MovingSprite : AnimatedSprite {
    public Vector2 Velocity = new Vector2(0, 0);
    public override void _PhysicsProcess(float delta) {
        Position = Position + Velocity;
        if (Position.x > 1040) {
            Position = new Vector2(-30, Position.y);
        }
        if (Position.x < -30) {
            Position = new Vector2(1040, Position.y);
        }
        if (Position.y > 800) {
            Position = new Vector2(Position.x, -30);
        }
        if (Position.y < -30) {
            Position = new Vector2(Position.x, 800);
        }
    }
}

