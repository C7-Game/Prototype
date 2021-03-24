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
        public bool IsLand { get; private set; }
        public int X { get; private set; }
        public int Y { get; private set; }
    }
    public override void _Ready()
    {
        GD.Print("TempTiles script started!");
        // Create reference to child node so we can change its settings from here
        Dialog = GetNode<FileDialog>("FileDialog");
        Dialog.CurrentDir = Util.GetCiv3Path() + @"/Conquests/Saves/Auto";

        LegacyMapReader = new ReadCivData.QueryCiv3Sav.Civ3File();
    }

    public void _on_OpenFileButton_pressed()
    {
        GD.Print("Open button pressed!");
        Dialog.Popup_();
    }

    public void _on_QuitButton_pressed()
    {
        GD.Print("Quit button pressed!");
        // NOTE: I think this quits the current node or scene and not necessarily the whole program if this is a child node?
        GetTree().Quit();
    }

    public void _on_FileDialog_file_selected(string path)
    {
        GD.Print("File selected! " + path);
        LegacyMapReader.Load(path);
        CreateTileSet();
    }
    private void CreateTileSet()
    {
        GD.Print("CreateTileSet()");
        Tiles = new List<TempTile>();
        int Offset = LegacyMapReader.SectionOffset("WRLD", 2) + 8;
        int WorldHeight = LegacyMapReader.ReadInt32(Offset);
        int WorldWidth = LegacyMapReader.ReadInt32(Offset + 5*4);
        GD.Print("World Size:");
        GD.Print(WorldWidth + " x " + WorldHeight); 
    }
}
