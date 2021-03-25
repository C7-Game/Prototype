using Godot;
using ConvertCiv3Media;
public class ViewPcxFlc : Node2D
{
    private FileDialog Dialog;
    private Node2D ViewImage;
    public override void _Ready()
    {
        // Create reference to child node so we can change its settings from here
        Dialog = GetNode<FileDialog>("FileDialog");
        Dialog.CurrentDir = Util.GetCiv3Path();
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
        GD.Print("Hi");
        if (ViewImage != null)
        {
            RemoveChild(ViewImage);
            // TODO: Remove old ViewImage (since Godot is C++, don't think just removing ref will suffice)
        }
        Pcx PcxTexture = new Pcx(path);
        Sprite PcxImage = new Sprite();
        PcxImage.Texture = PCXToGodot.getImageTextureFromPCX(PcxTexture);
        PcxImage.Position = new Vector2(200, 100);
        ViewImage = PcxImage;
        AddChild(ViewImage);
        Update();
    }
}
