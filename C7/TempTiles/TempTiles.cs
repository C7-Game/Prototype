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
}
