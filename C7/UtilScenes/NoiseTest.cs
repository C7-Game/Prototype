using Godot;
using System;

public class NoiseTest : Node2D
{
    [Export]
    int Width;
    [Export]
    int Height;
    [Export]
    bool wrapX;
    [Export]
    bool wrapY;
    [Export]
    double Persistence;
    [Export]
    int Octaves;
    public override void _Ready()
    {
        GD.Print(Width, Height);
        Position = new Vector2((Width/(float)2), Height/(float)2);
        double[,] tempNoiseField = C7GameData.GameMap.tempMapGenPrototyping(Width, Height, wrapX, wrapY);

        Image img = new Image();
        img.Create(Width, Height, false, Image.Format.L8);
        img.Lock();
        for (int x=0;x<Width;x++)
            for (int y=0;y<Height;y++)
            {
                float foo = 1 + ((float)tempNoiseField[x,y] / 2);
                foo = (float)tempNoiseField[x,y];
                img.SetPixel(x,y,new Color(foo, foo, foo, 1));    
            }
        img.Unlock();

        Sprite noiseSprite = new Sprite();
        AddChild(noiseSprite);
        ImageTexture txt = new ImageTexture();
        noiseSprite.Texture = txt;

        txt.CreateFromImage(img);
    }
}
