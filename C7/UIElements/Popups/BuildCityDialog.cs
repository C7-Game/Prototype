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

        //Need real buttons here for confirm/cancel.
    }

    private void OnCityNameEntered(string name)
    {
		GetTree().SetInputAsHandled();
        GD.Print("The user hit enter with a city name of " + name);
        GetParent().EmitSignal("BuildCity", name);
        GetParent().EmitSignal("hide");
    }

    public override void _UnhandledInput(InputEvent @event)
	{
		if (this.Visible) {
			if (@event is InputEventKey eventKey && eventKey.Pressed)
			{
				if (eventKey.Scancode == (int)Godot.KeyList.Escape)
				{
					GetTree().SetInputAsHandled();
                    GetParent().EmitSignal("hide");
				}
                else if (eventKey.Scancode ==(int)Godot.KeyList.Enter)
                {
                    GD.Print("The user hit enter with a city name of " + cityName.Text);
                    GetTree().SetInputAsHandled();
                    GetParent().EmitSignal("BuildCity", cityName.Text);
                    GetParent().EmitSignal("hide");
                }
			}
		}
	}
}