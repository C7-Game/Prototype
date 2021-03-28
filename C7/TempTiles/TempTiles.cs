using Godot;
using System;
using System.Collections.Generic;

public class TempTiles : Node2D
{
    private FileDialog Dialog;
    private QueryCiv3.Civ3File LegacyMapReader;
    private List<TempTile> Tiles;
    private class TempTile: LegacyMap.ILegacyTile
    {
        /*
        public int LegacyBaseTerrainID { get; set; }
        public int LegacyOverlayTerrainID { get; set; }
        */
        public int LegacyX { get; set; }
        public int LegacyY { get; set; }
        public int LegacyFileID { get; set; }
        public int LegacyImageID { get; set; }
    }
    private LegacyMap MapUI;
    private int Offset = 0;
    private Button OffsetButton;
    public override void _Ready()
    {
        // Create reference to child node so we can change its settings from here
        Dialog = GetNode<FileDialog>("FileDialog");
        Dialog.CurrentDir = Util.GetCiv3Path() + @"/Conquests/Saves";
        Dialog.Resizable = true;

        OffsetButton = GetNode<Button>("OffsetButton");

        LegacyMapReader = new QueryCiv3.Civ3File();
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

    public void _on_OffsetButton_pressed()
    {
        GD.Print("Offset button!");
        Offset++;
        OffsetButton.Text = "Offset " + Offset.ToString();
    }
    public void _on_OffsetMinusButton_pressed()
    {
        GD.Print("Offset Minus button!");
        Offset--;
        OffsetButton.Text = "Offset " + Offset.ToString();
    }

    public void _on_FileDialog_file_selected(string path)
    {
        LegacyMapReader.Load(path);
        CreateTileSet();
        MapUI.LegacyTiles = Tiles;
        MapUI.TerrainAsTileMap();
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
                ThisTile.LegacyX = x;
                ThisTile.LegacyY = y;

                /*
                int TerrainByte = LegacyMapReader.ReadByte(Offset+53);
                ThisTile.LegacyBaseTerrainID = TerrainByte & 0x0F;
                ThisTile.LegacyOverlayTerrainID = TerrainByte >> 4;
                */
                ThisTile.LegacyFileID = LegacyMapReader.ReadByte(Offset+11);
                ThisTile.LegacyImageID = LegacyMapReader.ReadByte(Offset+10);

                Tiles.Add(ThisTile);
                // 212 bytes per tile in Conquests SAV
                Offset += 212;
            }
        }
    }
}
