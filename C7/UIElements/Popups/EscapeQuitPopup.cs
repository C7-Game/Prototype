using Godot;

public class EscapeQuitPopup : TextureRect
{
	readonly int BUTTON_LABEL_OFFSET = 4;
    ImageTexture InactiveButton;
	ImageTexture HoverButton;
	StyleBoxFlat TransparentBackgroundStyle = new StyleBoxFlat();
	StyleBoxFlat TransparentBackgroundHoverStyle = new StyleBoxFlat();

    public override void _Ready()
    {
        base._Ready();

		InactiveButton = Util.LoadTextureFromPCX("Art/buttonsFINAL.pcx", 1, 1, 20, 20);
		HoverButton = Util.LoadTextureFromPCX("Art/buttonsFINAL.pcx", 22, 1, 20, 20);

		TransparentBackgroundStyle.BgColor = new Color(0, 0, 0, 0);
		TransparentBackgroundHoverStyle.BgColor = new Color(0, 0, 0, 0);

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
        
		AddButton("No, not really", 188, "cancel");
		AddButton("Yes, immediately!", 216, "quit");
    }
    private void quit()
	{
		GetParent().EmitSignal("Quit");
	}

	private void cancel()
	{
		GetParent().EmitSignal("hide");
	}

    /**
     * REFACTOR, DEDUPLICATE
     **/
    private void AddButton(string label, int verticalPosition, string actionName)
	{
		const int HORIZONTAL_POSITION = 30;
		TextureButton newButton = new TextureButton();
		newButton.TextureNormal = InactiveButton;
		newButton.TextureHover = HoverButton;
		newButton.SetPosition(new Vector2(HORIZONTAL_POSITION, verticalPosition));
		this.AddChild(newButton);
		newButton.Connect("pressed", this, actionName);
				
		Button newButtonLabel = new Button();
		newButtonLabel.Text = label;

		newButtonLabel.AddColorOverride("font_color", new Color(0, 0, 0));
		newButtonLabel.AddColorOverride("font_color_hover", Color.Color8(255, 0, 0));
		newButtonLabel.AddColorOverride("font_color_pressed", Color.Color8(0, 255, 0));	//when actively being clicked
		//Haven't figured out how to set the color after you've clicked on something (i.e. made it focused)

		newButtonLabel.AddStyleboxOverride("normal", TransparentBackgroundStyle);
		newButtonLabel.AddStyleboxOverride("hover", TransparentBackgroundHoverStyle);
		newButtonLabel.AddStyleboxOverride("pressed", TransparentBackgroundHoverStyle);

		newButtonLabel.SetPosition(new Vector2(HORIZONTAL_POSITION + 25, verticalPosition + BUTTON_LABEL_OFFSET));
		this.AddChild(newButtonLabel);
		newButtonLabel.Connect("pressed", this, actionName);
	}
}