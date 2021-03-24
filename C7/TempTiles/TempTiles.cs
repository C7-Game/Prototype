using Godot;
using System;
using System.Collections.Generic;

public class TempTiles : Node2D
{
    private FileDialog Dialog;
    private ReadCivData.QueryCiv3Sav.Civ3File LegacyMapReader;
    private List<TempTile> Tiles;
    public class TempTile: LegacyMap.ILegacyTile
    {
        public bool IsLand { get; set; }
        public int X { get; set; }
        public int Y { get; set; }
    }
    private LegacyMap MapUI;
    public override void _Ready()
    {
        // Create reference to child node so we can change its settings from here
        Dialog = GetNode<FileDialog>("FileDialog");
        Dialog.CurrentDir = Util.GetCiv3Path() + @"/Conquests/Saves";

        LegacyMapReader = new ReadCivData.QueryCiv3Sav.Civ3File();
        // Load LegacyMap scene (?) and attach to tree
        MapUI = new LegacyMap();
        this.AddChild(MapUI);
    }

    public void _on_OpenFileButton_pressed()
    {
        Dialog.Popup_();
    }

    public void _on_QuitButton_pressed()
    {
        // NOTE: I think this quits the current node or scene and not necessarily the whole program if this is a child node?
        GetTree().Quit();
    }

    public void _on_FileDialog_file_selected(string path)
    {
        LegacyMapReader.Load(path);
        CreateTileSet();
        MapUI.LegacyTiles = Tiles;
        MapUI.Update();
    }
    private void CreateTileSet()
    {
        Tiles = new List<TempTile>();
        int Offset = LegacyMapReader.SectionOffset("WRLD", 2) + 8;
        int WorldHeight = LegacyMapReader.ReadInt32(Offset);
        int WorldWidth = LegacyMapReader.ReadInt32(Offset + 5*4);

        Offset = LegacyMapReader.SectionOffset("TILE", 1);
        for (int y=0; y < WorldHeight; y++)
        {
            for (int x=y%2; x < WorldWidth; x+=2)
            {
                TempTile ThisTile = new TempTile();
                ThisTile.X = x;
                ThisTile.Y = y;
                // If low nybble of terrain byte is < 11, tile is land
                ThisTile.IsLand = (LegacyMapReader.ReadByte(Offset+53) & 0x0F) < 11;
                Tiles.Add(ThisTile);
                // 212 bytes per tile in Conquests SAV
                Offset += 212;
            }
        }
    }
}
