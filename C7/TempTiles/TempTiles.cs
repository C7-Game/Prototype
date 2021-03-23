using Godot;
using System;

public class TempTiles : Node2D
{
    public string SavePath = Util.GetCiv3Path() + @"/Conquests/Saves";
    public override void _Ready()
    {
        GD.Print("TempTiles script started!");
    }

    public void _on_OpenFileButton_pressed()
    {
        GD.Print("Open button pressed!");
        FileDialog Dialog = GetNode<FileDialog>("FileDialog");
        Dialog.CurrentDir = SavePath;
        Dialog.Popup_();
    }
}
