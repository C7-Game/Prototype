using Godot;
using System;
using System.Collections.Generic;

public class TempTiles : Node2D
{
    private class TextLayerClass : Node2D
    {
        public List<TempTile> Tiles;

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
            MapFont.Size = 32;
            if(Tiles != null)
            {
                foreach (TempTile tile in Tiles)
                {
                    // TODO: This is writing under the map and buttons for some reason
                    DrawString(MapFont, new Vector2(tile.LegacyX * 64 + 32, tile.LegacyY * 32 + 80), tile.DebugByte.ToString(), new Color(0,0,0,1));
                }
            }
        }
    }
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
        public int DebugByte;
    }
    private LegacyMap MapUI;
    private int TileOffset = 0;
    private Button OffsetButton;
    private DynamicFont MapFont;
    private float ScaleFactor = (float)0.25;
    private TextLayerClass DebugTextLayer;
    private float MapAlpha = (float)1;

    public override void _Ready()
    {
        string FontPath = Util.GetCiv3Path() + @"/LSANS.TTF";
        MapFont = new DynamicFont();
        MapFont.FontData = ResourceLoader.Load(FontPath) as DynamicFontData;

        // Create reference to child node so we can change its settings from here
        Dialog = GetNode<FileDialog>("FileDialog");
        Dialog.CurrentDir = Util.GetCiv3Path() + @"/Conquests/Saves";
        Dialog.Resizable = true;

        OffsetButton = GetNode<Button>("OffsetButton");

        LegacyMapReader = new QueryCiv3.Civ3File();
        // Load LegacyMap scene (?) and attach to tree
        MapUI = new LegacyMap();
        MapUI.Modulate = new Color(1,1,1,MapAlpha);
        this.AddChild(MapUI);
        DebugTextLayer = new TextLayerClass();
        DebugTextLayer.Scale = new Vector2(1, 1) * ScaleFactor;
        this.AddChild(DebugTextLayer);
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
        TileOffset++;
        OffsetButton.Text = "Offset " + TileOffset.ToString();
        CreateTileSet();
        Update();
    }
    public void _on_OffsetMinusButton_pressed()
    {
        TileOffset--;
        OffsetButton.Text = "Offset " + TileOffset.ToString();
        CreateTileSet();
        Update();
    }

    public void _on_FileDialog_file_selected(string path)
    {
        LegacyMapReader.Load(path);
        CreateTileSet();
        MapUI.LegacyTiles = Tiles;
        MapUI.TerrainAsTileMap();
        MapUI.Scale = new Vector2(1,1) * ScaleFactor;
        Update();
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
                ThisTile.DebugByte = LegacyMapReader.ReadByte(Offset+TileOffset);
                ThisTile.LegacyFileID = LegacyMapReader.ReadByte(Offset+17);
                ThisTile.LegacyImageID = LegacyMapReader.ReadByte(Offset+16);

                Tiles.Add(ThisTile);
                // 212 bytes per tile in Conquests SAV
                Offset += 212;
            }
        }
        DebugTextLayer.Visible = TileOffset != 0;
        DebugTextLayer.Tiles = Tiles;
        DebugTextLayer.Update();
    }
}
