using Godot;
using Serilog;

public partial class BuildCityDialog : Popup {

	LineEdit cityName = new LineEdit();
	private string defaultName = "";

	private ILogger log = LogManager.ForContext<BuildCityDialog>();

	public BuildCityDialog(string defaultName) {
		cityName.Theme = ThemeFactory.DefaultTheme;
		cityName.CaretBlink = true;
		this.defaultName = defaultName;
		alignment = BoxContainer.AlignmentMode.End;
		margins = new Margins(right: -10); // 10px margin from the right
	}

	public override void _Ready() {
		base._Ready();

		//Dimensions are 530x260 (roughly).
		//The top 110 px are for the advisor.

		AddTexture(530, 260);

		TextureRect advisorHead = new();
		advisorHead.Texture = AdvisorHead.GetPopupImage(AdvisorHead.Advisor.Culture, AdvisorHead.Mood.Happy, eraIndex: 0);
		//Appears at 400, 110 in game, but leftmost 25px are transparent with default graphics
		advisorHead.SetPosition(new Vector2(375, 0));
		AddChild(advisorHead);

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

		cityName.TextSubmitted += OnCityNameEntered;

		AddConfirmButton(new Vector2(475, 213), OnConfirmButtonPressed);
		AddCancelButton(new Vector2(500, 213));
	}

	/**
	 * Need a second method b/c the LineEdit sends a param and the ConfirmButton doesn't.
	 **/
	public void OnConfirmButtonPressed() {
		this.OnCityNameEntered(cityName.Text);
	}

	public void OnCityNameEntered(string name) {
		GetViewport().SetInputAsHandled();
		log.Debug("The user hit enter with a city name of " + name);
		GetParent().EmitSignal(PopupOverlay.SignalName.BuildCity, name);
		GetParent().EmitSignal(PopupOverlay.SignalName.HidePopup);
	}

}
