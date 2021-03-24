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
    private System.Collections.Generic.IEnumerable<ILegacyTile> LegacyTiles;
    public override void _Ready()
    {
        System.Collections.Generic.List<TempHackTile> foo = new System.Collections.Generic.List<TempHackTile>();
        foo.Add(new TempHackTile());
        LegacyTiles = foo;
        Temp();
    }
    public void Temp()
    {
        GD.Print("Temp() started");
        int TileCount = 0;
        foreach (ILegacyTile tile in LegacyTiles)
        {
            GD.Print(tile.X, tile.Y, tile.IsLand);
            TileCount++;
        }
        GD.Print("Total tiles: " + TileCount);
    }
}
