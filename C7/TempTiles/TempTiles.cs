using Godot;
using System;

public class TempTiles : Node2D
{
    public string SavePath = Util.GetCiv3Path() + @"/Conquests/Saves";
    private FileDialog Dialog;

    public override void _Ready()
    {
        GD.Print("TempTiles script started!");
        Dialog = GetNode<FileDialog>("FileDialog");
        Dialog.CurrentDir = SavePath;
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
}
