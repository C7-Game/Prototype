using Godot;

public class DisbandConfirmation : TextureRect
{
	public DisbandConfirmation() 
	{

	}
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

		//Dimensions in-game are 530x320
		//The top 110px are for the advisor leaderhead, Domestic in this case.
		//For some reason it uses the Happy graphics.

		//Create a transparent texture background of the appropriate size.
		//This is super important as if we just add the children, the parent won't be able to figure
		//out the size of this TextureRect, and it won't be able to align it properly.
		//I added an extra 10 px on width for margin... maybe we should do margin another way, but 
		//this works reliably.
		ImageTexture thisTexture = new ImageTexture();
		Image image = new Image();
		image.Create(540, 320, false, Image.Format.Rgba8);
		image.Fill(Color.Color8(0, 0, 0, 0));
		thisTexture.CreateFromImage(image);
		this.Texture = thisTexture;


		ImageTexture AdvisorHappy = Util.LoadTextureFromPCX("Art/SmallHeads/popupDOMESTIC.pcx", 1, 40, 149, 110);
		TextureRect AdvisorHead = new TextureRect();
		AdvisorHead.Texture = AdvisorHappy;
		//Appears at 400, 110 in game, but leftmost 25px are transparent with default graphics
		AdvisorHead.SetPosition(new Vector2(375, 0));
		AddChild(AdvisorHead);

		TextureRect background = PopupOverlay.GetPopupBackground(530, 210);
		background.SetPosition(new Vector2(0, 110));
		AddChild(background);

		PopupOverlay.AddHeaderToPopup(this, "Domestic Advisor", 120);

		Label warningMessage = new Label();
		//TODO: General-purpose text breaking up util.  Instead of \n
		//This appears to be the way to do multi line labels, see: https://godotengine.org/qa/11126/how-to-break-line-on-the-label-using-gdscript
		//Maybe there's an awesomer control we can user instead
		warningMessage.AddColorOverride("font_color", new Color(0, 0, 0));
		warningMessage.Text = "Disband Settler?  Pardon me but these are OUR people. Do \nyou really want to disband them?";

		warningMessage.SetPosition(new Vector2(25, 170));
		AddChild(warningMessage);

		AddButton("Yes, we need to!", 215, "disband");
		AddButton("No. Maybe you are right, advisor.", 245, "cancel");
	}

	private void disband()
	{
		//tell the game to disband it.  right now we're doing that first, which is WRONG!
		GetParent().EmitSignal("UnitDisbanded");
		GetParent().EmitSignal("hide");
	}

	private void cancel()
	{
		GetParent().EmitSignal("hide");
	}

	/**
	 * This is yanked from MainMenu.  Should be refactored into a utility method because
	 * we will need something like it in a lot of places.
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
