using Godot;
using System.Collections.Generic;

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
    public IEnumerable<ILegacyTile> LegacyTiles;
    private DynamicFont MapFont;
    public override void _Ready()
    {
        string FontPath = Util.GetCiv3Path() + @"/LSANS.TTF";
        MapFont = new DynamicFont();
        MapFont.FontData = ResourceLoader.Load(FontPath) as DynamicFontData;
    }
    public override void _Draw()
    {
        base._Draw();
        MapFont.Size = 10;
        if(LegacyTiles != null)
        {
            foreach (ILegacyTile tile in LegacyTiles)
            {
                DrawString(MapFont, new Vector2(tile.X * 10, tile.Y * 5), tile.IsLand ? "O" : "", new Color(1,1,1,1));
            }
        }
    }
}
