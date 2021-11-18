using Godot;

public class EscapeQuitPopup : TextureRect
{

    public override void _Ready()
    {
        base._Ready();

        //Dimensions in-game are 370x295, centered at the top
		//The top 100px are empty (this is different than the 110px when there's an advisor)

		//Create a transparent texture background of the appropriate size.
		//This is super important as if we just add the children, the parent won't be able to figure
		//out the size of this TextureRect, and it won't be able to align it properly.
		ImageTexture thisTexture = new ImageTexture();
		Image image = new Image();
		image.Create(370, 295, false, Image.Format.Rgba8);
		image.Fill(Color.Color8(0, 0, 0, 0));
		thisTexture.CreateFromImage(image);
		this.Texture = thisTexture;

        TextureRect background = PopupOverlay.GetPopupBackground(370, 195);
		background.SetPosition(new Vector2(0, 100));
		AddChild(background);

		PopupOverlay.AddHeaderToPopup(this, "Oh No!", 110);

        Label warningMessage = new Label();
		//TODO: General-purpose text breaking up util.  Instead of \n
		//This appears to be the way to do multi line labels, see: https://godotengine.org/qa/11126/how-to-break-line-on-the-label-using-gdscript
		//Maybe there's an awesomer control we can user instead
		warningMessage.AddColorOverride("font_color", new Color(0, 0, 0));
		warningMessage.Text = "Do you really want to quit?";

		warningMessage.SetPosition(new Vector2(25, 162));
		AddChild(warningMessage);
        
		PopupOverlay.AddButton(this, "No, not really", 188, "cancel");
		PopupOverlay.AddButton(this, "Yes, immediately!", 216, "quit");
    }
    private void quit()
	{
		GetParent().EmitSignal("Quit");
	}

	private void cancel()
	{
		GetParent().EmitSignal("HidePopup");
	}
}