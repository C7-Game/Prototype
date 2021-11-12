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

		//The pop-up part is the tricky part
		ImageTexture topLeftPopup = Util.LoadTextureFromPCX("Art/popupborders.pcx", 251, 1, 61, 44);
		ImageTexture topCenterPopup = Util.LoadTextureFromPCX("Art/popupborders.pcx", 313, 1, 61, 44);
		ImageTexture topRightPopup = Util.LoadTextureFromPCX("Art/popupborders.pcx", 375, 1, 61, 44);
		ImageTexture middleLeftPopup = Util.LoadTextureFromPCX("Art/popupborders.pcx", 251, 46, 61, 44);
		ImageTexture middleCenterPopup = Util.LoadTextureFromPCX("Art/popupborders.pcx", 313, 46, 61, 44);
		ImageTexture middleRightPopup = Util.LoadTextureFromPCX("Art/popupborders.pcx", 375, 46, 61, 44);
		ImageTexture bottomLeftPopup = Util.LoadTextureFromPCX("Art/popupborders.pcx", 251, 91, 61, 44);
		ImageTexture bottomCenterPopup = Util.LoadTextureFromPCX("Art/popupborders.pcx", 313, 91, 61, 44);
		ImageTexture bottomRightPopup = Util.LoadTextureFromPCX("Art/popupborders.pcx", 375, 91, 61, 44);

		//Dimensions are 530x320.  The leaderhead takes up 110.  So the popup is 530x210.
		//We have multiples of... 62? For the horizontal dimension, 45 for vertical.
		//45 does not fit into 210.  90, 135, 180, 215.  Well, 215 is sorta closeish.
		//62, we got 62, 124, 248, 496, 558.  Doesn't match up at all.
		//Which means that partial textures can be used.  Lovely.

		//Let's try adding some helper functions so this can be refactored later into a more general-purpose popup popper
		int vOffset = 110;
		int height = 320;
		drawRowOfPopup(vOffset, 530, topLeftPopup, topCenterPopup, topRightPopup);
		const int VTILE_SIZE = 44;
		vOffset+=VTILE_SIZE;
		for (;vOffset < height - VTILE_SIZE; vOffset += VTILE_SIZE) {
			drawRowOfPopup(vOffset, 530, middleLeftPopup, middleCenterPopup, middleRightPopup);
		}
		vOffset = height - VTILE_SIZE;
		drawRowOfPopup(vOffset, 530, bottomLeftPopup, bottomCenterPopup, bottomRightPopup);


		//Pop-up done.  Should refactor it someday so it's reusable.  But for now let's add the other things and stuff
		HBoxContainer header = new HBoxContainer();
		header.Alignment = BoxContainer.AlignMode.Center;
		Label advisorType = new Label();
		advisorType.AddColorOverride("font_color", new Color(0, 0, 0));
		//Set the font size.  For labels, there is no one-off override, so we have to
		//set it on a theme like this.
		//The SetFont arguments aren't documented in a way that a non-Godot expert can understand
		//My current understanding is that we need to set the first parameter to "font", and the
		//second to whatever type it should apply to.  But that is based on nothing official.
		//Also you can set the size with bigFont.Size = 72, but that applies everywhere the font
		//is used in the whole program.  Not recommended.
		DynamicFont bigFont = ResourceLoader.Load<DynamicFont>("res://Fonts/NSansFont24Pt.tres");
		Theme theme = new Theme();
		theme.SetFont("font", "Label", bigFont);
		advisorType.Theme = theme;
		advisorType.Text = "Domestic Advisor";
		header.AddChild(advisorType);
		header.SetPosition(new Vector2(0, 120));
		header.AnchorLeft = 0.0f;
		header.AnchorRight = 1.0f;
		header.MarginRight = 10;    //For some reason this isn't causing it to be indented 10 pixels from the right.  Uncomment the line above and you'll see the tooltip goes all the way across
		AddChild(header);

		Label warningMessage = new Label();
		//TODO: General-purpose text breaking up util.  Instead of \n
		//This appears to be the way to do multi line labels, see: https://godotengine.org/qa/11126/how-to-break-line-on-the-label-using-gdscript
		//Maybe there's an awesomer control we can user instead
		warningMessage.AddColorOverride("font_color", new Color(0, 0, 0));
		warningMessage.Text = "Disband Settler?  Pardon me but these are OUR people. Do \nyou really want to disband them?";
		
		// HBoxContainer messageCenteringContainer = new HBoxContainer();
		// messageCenteringContainer.Alignment = BoxContainer.AlignMode.Center;
		// messageCenteringContainer.SetPosition(new Vector2(0, 170));
		// messageCenteringContainer.AnchorLeft = 0.05f;
		// messageCenteringContainer.AnchorRight = 0.95f;
		// messageCenteringContainer.AddChild(warningMessage);
		// AddChild(messageCenteringContainer);

		warningMessage.SetPosition(new Vector2(25, 170));
		AddChild(warningMessage);

		//30, 215
		//50, 215

		//and 245

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
		//Only the Exit and part of the Credits button are getting the right color.  The rest are black.  Not sure why.
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

	private void drawRowOfPopup(int vOffset, int width, ImageTexture left, ImageTexture center, ImageTexture right)
	{
		//Okay, at least we only need one function for all three rows, it can be SIMD (single instruction, multiple data) by analogy
		TextureRect leftRectangle = new TextureRect();
		leftRectangle.SetPosition(new Vector2(0, vOffset));
		leftRectangle.Texture = left;
		AddChild(leftRectangle);

		const int TILE_SIZE = 61;   //yes, it will always be 61.  at least with Civ graphics.  so like WildWeazel, it will be hard coded
		int leftOffset = TILE_SIZE;
		for (;leftOffset < width - TILE_SIZE; leftOffset += TILE_SIZE)
		{
			TextureRect middleRectangle = new TextureRect();
			middleRectangle.SetPosition(new Vector2(leftOffset, vOffset));
			middleRectangle.Texture = center;
			AddChild(middleRectangle);
		}

		leftOffset = width - TILE_SIZE;
		TextureRect rightRectangle = new TextureRect();
		rightRectangle.SetPosition(new Vector2(leftOffset, vOffset));
		rightRectangle.Texture = right;
		AddChild(rightRectangle);
	}
}
