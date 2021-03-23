using Godot;
using System;

public class TempTiles : Node2D
{
    public override void _Ready()
    {
        GD.Print("TempTiles script started!");
        string path = Util.GetCiv3Path() + @"/Conquests/Saves";
    }

    public void _on_OpenFileButton_pressed()
    {
        GD.Print("Open button pressed!");
        FileDialog Dialog = GetNode<FileDialog>("FileDialog");
        Dialog.Popup_();
    }
}
