using Godot;

public class BuildCityDialog : TextureRect
{
	
	LineEdit cityName = new LineEdit();
	
	public override void _Ready()
	{
		base._Ready();

		//Dimensions are 530x260 (roughly).
		//The top 110 px are for the advisor.

		//Transparent background.  Add 10 px on the right for offset.
		ImageTexture thisTexture = new ImageTexture();
		Image image = new Image();
		image.Create(540, 260, false, Image.Format.Rgba8);
		image.Fill(Color.Color8(0, 0, 0, 0));
		thisTexture.CreateFromImage(image);
		this.Texture = thisTexture;

		ImageTexture AdvisorHappy = Util.LoadTextureFromPCX("Art/SmallHeads/popupCULTURE.pcx", 1, 40, 149, 110);
		TextureRect AdvisorHead = new TextureRect();
		AdvisorHead.Texture = AdvisorHappy;
		//Appears at 400, 110 in game, but leftmost 25px are transparent with default graphics
		AdvisorHead.SetPosition(new Vector2(375, 0));
		AddChild(AdvisorHead);

		TextureRect background = PopupOverlay.GetPopupBackground(530, 150);
		background.SetPosition(new Vector2(0, 110));
		AddChild(background);

		PopupOverlay.AddHeaderToPopup(this, "Name this town?", 120);

		HBoxContainer labelAndName = new HBoxContainer();
		labelAndName.Alignment = BoxContainer.AlignMode.Begin;
		labelAndName.SizeFlagsHorizontal = 3;   //fill and expand
		labelAndName.SizeFlagsStretchRatio = 1;
		labelAndName.AnchorLeft = 0.0f;
		labelAndName.AnchorRight = 0.85f;
		labelAndName.SetPosition(new Vector2(30, 170));

		Label nameLabel = new Label();
		nameLabel.AddColorOverride("font_color", new Color(0, 0, 0));
		nameLabel.Text = "Name: ";
		labelAndName.AddChild(nameLabel);

		cityName.SizeFlagsHorizontal = 3;  //fill and expand
		cityName.SizeFlagsStretchRatio = 1;
		cityName.Text = "Hippo Regius";
		labelAndName.AddChild(cityName);

		this.AddChild(labelAndName);

		cityName.SelectAll();
		cityName.GrabFocus();

		cityName.Connect("text_entered", this, "OnCityNameEntered");

		//Cancel/confirm buttons.  Note the X button is thinner than the O button.
		ImageTexture circleTexture= Util.LoadTextureFromPCX("Art/X-o_ALLstates-sprite.pcx", 1, 1, 19, 19);
		ImageTexture xTexture = Util.LoadTextureFromPCX("Art/X-o_ALLstates-sprite.pcx", 21, 1, 15, 19);
		ImageTexture circleHover = Util.LoadTextureFromPCX("Art/X-o_ALLstates-sprite.pcx", 37, 1, 19, 19);
		ImageTexture xHover = Util.LoadTextureFromPCX("Art/X-o_ALLstates-sprite.pcx", 57, 1, 15, 19);
		ImageTexture circlePressed = Util.LoadTextureFromPCX("Art/X-o_ALLstates-sprite.pcx", 73, 1, 19, 19);
		ImageTexture xPressed = Util.LoadTextureFromPCX("Art/X-o_ALLstates-sprite.pcx", 93, 1, 15, 19);
		TextureButton confirmButton = new TextureButton();
		confirmButton.TextureNormal = circleTexture;
		confirmButton.TextureHover = circleHover;
		confirmButton.TexturePressed = circlePressed;
		confirmButton.SetPosition(new Vector2(475, 213));
		AddChild(confirmButton);
		TextureButton cancelButton = new TextureButton();
		cancelButton.TextureNormal = xTexture;
		cancelButton.TextureHover = xHover;
		cancelButton.TexturePressed = xPressed;
		cancelButton.SetPosition(new Vector2(500, 213));
		AddChild(cancelButton);

		confirmButton.Connect("pressed", this, "OnConfirmButtonPressed");
		cancelButton.Connect("pressed", GetParent(), "OnHidePopup");
	}

	/**
	 * Need a second method b/c the LineEdit sends a param and the ConfirmButton doesn't.
	 **/
	public void OnConfirmButtonPressed()
	{
		this.OnCityNameEntered(cityName.Text);
	}

	public void OnCityNameEntered(string name)
	{
		GetTree().SetInputAsHandled();
		GD.Print("The user hit enter with a city name of " + name);
		GetParent().EmitSignal("BuildCity", name);
		GetParent().EmitSignal("HidePopup");
	}

	public override void _UnhandledInput(InputEvent @event)
	{
		if (this.Visible) {
			if (@event is InputEventKey eventKey && eventKey.Pressed)
			{
				if (eventKey.Scancode == (int)Godot.KeyList.Escape)
				{
					GetTree().SetInputAsHandled();
					GetParent().EmitSignal("HidePopup");
				}
			}
		}
	}
}
