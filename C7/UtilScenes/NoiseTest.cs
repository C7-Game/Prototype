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
        double sum = 0, min = 0, max = 0;
        int count = 0;
        int countGtOne = 0;
        int countLtNOne = 0;

        Position = new Vector2((Width/(float)2), Height/(float)2);
        double[,] tempNoiseField = C7GameData.GameMap.tempMapGenPrototyping(Width, Height, wrapX, wrapY);
        min = tempNoiseField[0,0];
        max = min;

        Image img = new Image();
        img.Create(Width, Height, false, Image.Format.L8);
        img.Lock();
        for (int x=0;x<Width;x++)
            for (int y=0;y<Height;y++)
            {
                // The public domain OpenSimplex code seems to return between -0.5 and 0.5 with one octave
                float foo = (float)(tempNoiseField[x,y]) + (float)0.5;
                img.SetPixel(x,y,new Color(foo, foo, foo, 1));
                sum += foo;
                count++;
                if (foo>max) max = foo;
                if (foo<min) min = foo;
                if (foo>1) countGtOne++;
                if (foo<-1) countLtNOne++;

            }
        img.Unlock();

        Sprite noiseSprite = new Sprite();
        AddChild(noiseSprite);
        ImageTexture txt = new ImageTexture();
        noiseSprite.Texture = txt;

        txt.CreateFromImage(img);
        GD.Print(sum/count);
        GD.Print(max);
        GD.Print(min);
        GD.Print(countGtOne/(double)count);
        GD.Print(countLtNOne/(double)count);
    }
}
