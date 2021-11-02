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
            MapFont = ResourceLoader.Load<DynamicFont>("res://Fonts/NSansFont24Pt.tres");

        }
        public override void _Draw()
        {
            base._Draw();
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
    private Util.Civ3FileDialog Dialog;
    private QueryCiv3.Civ3File LegacyMapReader;
    private List<TempTile> Tiles;
    private class TempTile: LegacyMap.ILegacyTile
    {
        public int LegacyBaseTerrainID { get; set; }
        public int LegacyOverlayTerrainID { get; set; }
        public int LegacyX { get; set; }
        public int LegacyY { get; set; }
        public int LegacyFileID { get; set; }
        public int LegacyImageID { get; set; }
        public int DebugByte;
    }
    private LegacyMap MapUI;
    private int TileOffset = 0;
    private DynamicFont MapFont;
    private TextLayerClass DebugTextLayer;
    private bool MoveCamera;
    private Vector2 OldPosition;
    private KinematicBody2D Player;

    public override void _Ready()
    {
        string FontPath = Util.GetCiv3Path() + @"/LSANS.TTF";
        MapFont = new DynamicFont();
        MapFont.FontData = ResourceLoader.Load(FontPath) as DynamicFontData;

        Dialog = new Util.Civ3FileDialog();
        Dialog.RelPath = @"Conquests/Saves";
        Dialog.Connect("file_selected", this, nameof(_on_FileDialog_file_selected));
        GetNode<Control>("CanvasLayer/ToolBar").AddChild(Dialog);

        // Load LegacyMap scene (?) and attach to tree
        MapUI = new LegacyMap();
        this.AddChild(MapUI);
        DebugTextLayer = new TextLayerClass();
        this.AddChild(DebugTextLayer);

        Player = GetNode<KinematicBody2D>("KinematicBody2D");
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

    public void _on_SpinBox_value_changed(float value)
    {
        TileOffset = (int)value;
        // TODO: check if file loaded before running this
        CreateTileSet();
        Update();
    }
    public void _on_Zoom_value_changed(float value)
    {
        Vector2 NewScale = new Vector2(value, value);
        Scale = NewScale;
    }
    public void _on_RightButton_pressed()
    {
        Player.Position = new Vector2(Player.Position.x + 128, Player.Position.y);
    }
    public void _on_LeftButton_pressed()
    {
        Player.Position = new Vector2(Player.Position.x - 128, Player.Position.y);
    }
    public void _on_UpButton_pressed()
    {
        Player.Position = new Vector2(Player.Position.x, Player.Position.y - 64);
    }
    public void _on_DownButton_pressed()
    {
        Player.Position = new Vector2(Player.Position.x, Player.Position.y + 64);
    }
    public void _on_FileDialog_file_selected(string path)
    {
        LegacyMapReader = new QueryCiv3.Civ3File(QueryCiv3.Util.ReadFile(path));
        CreateTileSet();
        MapUI.LegacyTiles = Tiles;
        MapUI.TerrainAsTileMap();
        Update();
    }
    public override void _UnhandledInput(InputEvent @event)
    {
        // Scrolls map by repositioning "Player" when clicking & dragging mouse
        // Control node must not be in the way and/or have mouse pass enabled
        if(@event is InputEventMouseButton eventMouseButton)
        {
            if(eventMouseButton.ButtonIndex == (int)ButtonList.Left)
            {
                GetTree().SetInputAsHandled();
                if(eventMouseButton.IsPressed())
                {
                    OldPosition = eventMouseButton.Position;
                    MoveCamera = true;
                }
                else
                {
                    MoveCamera = false;
                }
            }
            else if(eventMouseButton.ButtonIndex == (int)ButtonList.WheelUp)
            {
                GetTree().SetInputAsHandled();
                GetNode<HSlider>("CanvasLayer/ToolBar/MarginContainer/HBoxContainer/Zoom").Value += (float)0.1;
            }
            else if(eventMouseButton.ButtonIndex == (int)ButtonList.WheelDown)
            {
                GetTree().SetInputAsHandled();
                GetNode<HSlider>("CanvasLayer/ToolBar/MarginContainer/HBoxContainer/Zoom").Value -= (float)0.1;
            }
        }
        else if(@event is InputEventMouseMotion eventMouseMotion)
        {
            if(MoveCamera)
            {
                GetTree().SetInputAsHandled();
                Player.Position += (OldPosition - eventMouseMotion.Position) / Scale;
                OldPosition = eventMouseMotion.Position;
            }
        }
    }
    private void CreateTileSet()
    {
        // TODO: Pull mod path from embedded BIC if present
        if (LegacyMapReader.HasCustomBic)
        {
            QueryCiv3.Civ3File customBic = new QueryCiv3.Civ3File(LegacyMapReader.CustomBic);

            // MapUI.ModRelPath = "";
            // Unsure of length of this string field...or of starting offset
            MapUI.ModRelPath = LegacyMapReader.GetString(0x2f, 256);
        }
        else 
        {
            MapUI.ModRelPath = "";
            // MapUI.ModRelPath = @"civ3PTW/Scenarios/Sn00pys Terrain";
            // MapUI.ModRelPath = @"civ3PTW/Scenarios/Warpstorm Watercolor Terrain";
        }
        Tiles = new List<TempTile>();
        int Offset = LegacyMapReader.SectionOffset("WRLD", 2) + 8;
        MapUI.MapHeight = LegacyMapReader.ReadInt32(Offset);
        MapUI.MapWidth = LegacyMapReader.ReadInt32(Offset + 5*4);

        Offset = LegacyMapReader.SectionOffset("TILE", 1);
        for (int y=0; y < MapUI.MapHeight; y++)
        {
            for (int x=y%2; x < MapUI.MapWidth; x+=2)
            {
                TempTile ThisTile = new TempTile();
                ThisTile.LegacyX = x;
                ThisTile.LegacyY = y;

                int TerrainByte = LegacyMapReader.ReadByte(Offset+53);
                ThisTile.LegacyBaseTerrainID = TerrainByte & 0x0F;
                ThisTile.LegacyOverlayTerrainID = TerrainByte >> 4;
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
