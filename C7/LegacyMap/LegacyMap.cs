using Godot;
using System;

public class LegacyMap : Node2D
{
    public interface ILegacyTile
    // Tiles need to provide this info to LegacyMap
    {
        // temp hack for prototyping land/water only map
        bool IsLand {get;}
        int X {get;}
        int Y {get;}
        
    }
    private class TempHackTile: ILegacyTile
    {
        public bool IsLand
        {
            get { return true; }
        }
        public int X
        {
            get { return 0; }
        }
        public int Y
        {
            get { return 0; }
        }
    }
    public System.Collections.Generic.IEnumerable<ILegacyTile> LegacyTiles;
    private DynamicFont MapFont;
    public override void _Ready()
    {
        string FontPath = Util.GetCiv3Path() + @"/LSANS.TTF";
        MapFont = new DynamicFont();
        MapFont.FontData = ResourceLoader.Load(FontPath) as DynamicFontData;
        LegacyTiles = new System.Collections.Generic.List<TempHackTile>();
    }
    public override void _Draw()
    {
        base._Draw();
        MapFont.Size = 10;
        foreach (ILegacyTile tile in LegacyTiles)
        {
            DrawString(MapFont, new Vector2(tile.X * 10, tile.Y * 5), tile.IsLand ? "O" : "", new Color(1,1,1,1));
        }
    }
}
