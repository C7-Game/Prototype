using Godot;
using System;

public class TempTiles : Node2D
{
    private FileDialog Dialog;

    public override void _Ready()
    {
        GD.Print("TempTiles script started!");
        // Create reference to child node so we can change its settings from here
        Dialog = GetNode<FileDialog>("FileDialog");
        Dialog.CurrentDir = Util.GetCiv3Path() + @"/Conquests/Saves/Auto";
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
        // DoSomething();
    }
}
