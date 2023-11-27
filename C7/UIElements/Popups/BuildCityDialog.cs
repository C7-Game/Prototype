using Godot;
using Serilog;

public partial class BuildCityDialog : Popup
{

	LineEdit cityName = new LineEdit();
	private string defaultName = "";

	private ILogger log = LogManager.ForContext<BuildCityDialog>();

	public BuildCityDialog(string defaultName)
	{
		this.defaultName = defaultName;
		alignment = BoxContainer.AlignmentMode.End;
		margins = new Margins(right: -10); // 10px margin from the right
	}

	public override void _Ready()
	{
		base._Ready();

		//Dimensions are 530x260 (roughly).
		//The top 110 px are for the advisor.

		AddTexture(530, 260);

		ImageTexture AdvisorHappy = Util.LoadTextureFromPCX("Art/SmallHeads/popupCULTURE.pcx", 1, 40, 149, 110);
		TextureRect AdvisorHead = new TextureRect();
		AdvisorHead.Texture = AdvisorHappy;
		//Appears at 400, 110 in game, but leftmost 25px are transparent with default graphics
		AdvisorHead.SetPosition(new Vector2(375, 0));
		AddChild(AdvisorHead);

		AddBackground(530, 150, 110);

		AddHeader("Name this town?", 120);

		HBoxContainer labelAndName = new HBoxContainer();
		labelAndName.Alignment = BoxContainer.AlignmentMode.Begin;
		labelAndName.SizeFlagsHorizontal = SizeFlags.ExpandFill;
		labelAndName.SizeFlagsStretchRatio = 1;
		labelAndName.AnchorLeft = 0.0f;
		labelAndName.AnchorRight = 0.85f;
		labelAndName.SetPosition(new Vector2(30, 170));

		Label nameLabel = new Label();
		nameLabel.Text = "Name: ";
		labelAndName.AddChild(nameLabel);

		cityName.SizeFlagsHorizontal = SizeFlags.ExpandFill;
		cityName.SizeFlagsStretchRatio = 1;
		cityName.Text = defaultName;
		labelAndName.AddChild(cityName);

		this.AddChild(labelAndName);

		cityName.SelectAll();
		cityName.GrabFocus();

		cityName.Connect("text_submitted", new Callable(this, "OnCityNameEntered"));

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

		confirmButton.Connect("pressed",new Callable(this,"OnConfirmButtonPressed"));
		cancelButton.Connect("pressed",new Callable(GetParent(),"OnHidePopup"));
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
		GetViewport().SetInputAsHandled();
		log.Debug("The user hit enter with a city name of " + name);
		GetParent().EmitSignal("BuildCity", name);
		GetParent().EmitSignal("HidePopup");
	}

}
