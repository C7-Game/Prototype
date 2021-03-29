using Godot;
using ConvertCiv3Media;
public class ViewPcxFlc : Node2D
{
    private Util.Civ3FileDialog Dialog;
    private Node2D ViewImage;
    public override void _Ready()
    {
        Dialog = new Util.Civ3FileDialog();
        Dialog.Connect("file_selected", this, nameof(_on_FileDialog_file_selected));
        AddChild(Dialog);
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
        if (ViewImage != null)
        {
            RemoveChild(ViewImage);
            // TODO: Delete old ViewImage (since Godot is C++, don't think just removing ref will suffice)
        }
        Pcx PcxTexture = new Pcx(path);
        Sprite PcxImage = new Sprite();
        PcxImage.Texture = PCXToGodot.getImageTextureFromPCX(PcxTexture);
        // Sprite are positioned by center, so get size and offset to alighn to top-left of window
        Vector2 SpriteSize = PcxImage.GetRect().Size;
        PcxImage.Position = new Vector2(SpriteSize.x / 2, SpriteSize.y / 2);
        ViewImage = PcxImage;
        AddChild(ViewImage);
        Update();
    }
}
