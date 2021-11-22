using Godot;
using ConvertCiv3Media;
using System;
using System.Diagnostics;

public class PopupOverlay : HBoxContainer
{
	[Signal] public delegate void UnitDisbanded();
	[Signal] public delegate void Quit();
	[Signal] public delegate void BuildCity(string name);
	[Signal] public delegate void HidePopup();

	Control currentChild = null;
	
	const int HTILE_SIZE = 61;
	const int VTILE_SIZE = 44;
	private readonly static int BUTTON_LABEL_OFFSET = 4;
	private static ImageTexture InactiveButton;
	private static ImageTexture HoverButton;
	private static StyleBoxFlat TransparentBackgroundStyle = new StyleBoxFlat();
	private static StyleBoxFlat TransparentBackgroundHoverStyle = new StyleBoxFlat();

	public enum PopupCategory {
		Advisor,
		Console,
		Info	//Sounds similar to the above, but lower-pitched in the second half
	}
	public override void _Ready()
	{
		base._Ready();
		
		InactiveButton = Util.LoadTextureFromPCX("Art/buttonsFINAL.pcx", 1, 1, 20, 20);
		HoverButton = Util.LoadTextureFromPCX("Art/buttonsFINAL.pcx", 22, 1, 20, 20);
		
		TransparentBackgroundStyle.BgColor = new Color(0, 0, 0, 0);
		TransparentBackgroundHoverStyle.BgColor = new Color(0, 0, 0, 0);
	}
	
	private void OnHidePopup()
	{
		GD.Print("Hiding popup");
		this.RemoveChild(currentChild);
		this.Hide();
	}

	public void PlaySound(AudioStreamSample wav)
	{
		AudioStreamPlayer player = GetNode<AudioStreamPlayer>("PopupSound");
		player.Stream = wav;
		player.Play();
	}

	public void ShowPopup(string dialogType, PopupCategory popupCategory)
	{
		string[] args = {};
		this.ShowPopup(dialogType, popupCategory, AlignMode.End, args);
	}


	public void ShowPopup(string dialogType, PopupCategory popupCategory, string[] args)
	{
		this.ShowPopup(dialogType, popupCategory, AlignMode.End, args);
	}

	public void ShowPopup(string dialogType, PopupCategory popupCategory, AlignMode alignMode)
	{
		string[] args = {};
		this.ShowPopup(dialogType, popupCategory, alignMode, args);
	}

	public void ShowPopup(string dialogType, PopupCategory popupCategory, AlignMode alignMode, string[] args)
	{
		this.Alignment = alignMode;

		if (dialogType.Equals("disband")) {
			DisbandConfirmation dbc = new DisbandConfirmation(args[0]);
			AddChild(dbc);
			currentChild = dbc;
		}
		else if (dialogType.Equals("escapeQuit")) {
			EscapeQuitPopup eqp = new EscapeQuitPopup();
			AddChild(eqp);
			currentChild = eqp;
		}
		else if (dialogType.Equals("buildCity")) {
			BuildCityDialog bcd = new BuildCityDialog();
			AddChild(bcd);
			currentChild =bcd;
		}
		
		if (currentChild != null) {
			string soundFile = "";
			switch(popupCategory) {
				case PopupCategory.Advisor:
					soundFile = "Sounds/PopupAdvisor.wav";
					break;
				case PopupCategory.Console:
					soundFile = "Sounds/PopupConsole.wav";
					break;
				case PopupCategory.Info:
					soundFile = "Sounds/PopupInfo.wav";
					break;
				default:
					GD.PrintErr("Invalid popup category");
					break;
			}
			AudioStreamSample wav = Util.LoadWAVFromDisk(Util.Civ3MediaPath(soundFile));
			this.Visible = true;
			PlaySound(wav);
		}
		else {
			GD.PrintErr("Received request to show invalid dialog type " + dialogType);
		}
	}

	public static TextureRect GetPopupBackground(int width, int height)
	{		
		Image image = new Image();
		image.Create(width, height, false, Image.Format.Rgba8);

		Pcx popupborders = Util.LoadPCX("Art/popupborders.pcx");

		//The pop-up part is the tricky part
		Stopwatch imageTimer = new Stopwatch();
		imageTimer.Start();
		Image topLeftPopup      = PCXToGodot.getImageFromPCX(popupborders, 251, 1, 61, 44);
		Image topCenterPopup    = PCXToGodot.getImageFromPCX(popupborders, 313, 1, 61, 44);
		Image topRightPopup     = PCXToGodot.getImageFromPCX(popupborders, 375, 1, 61, 44);
		Image middleLeftPopup   = PCXToGodot.getImageFromPCX(popupborders, 251, 46, 61, 44);
		Image middleCenterPopup = PCXToGodot.getImageFromPCX(popupborders, 313, 46, 61, 44);
		Image middleRightPopup  = PCXToGodot.getImageFromPCX(popupborders, 375, 46, 61, 44);
		Image bottomLeftPopup   = PCXToGodot.getImageFromPCX(popupborders, 251, 91, 61, 44);
		Image bottomCenterPopup = PCXToGodot.getImageFromPCX(popupborders, 313, 91, 61, 44);
		Image bottomRightPopup  = PCXToGodot.getImageFromPCX(popupborders, 375, 91, 61, 44);
		imageTimer.Stop();
		TimeSpan stopwatchElapsed = imageTimer.Elapsed;
		GD.Print("Image creation time: " + Convert.ToInt32(stopwatchElapsed.TotalMilliseconds) + " ms");

		//Dimensions are 530x320.  The leaderhead takes up 110.  So the popup is 530x210.
		//We have multiples of... 62? For the horizontal dimension, 45 for vertical.
		//45 does not fit into 210.  90, 135, 180, 215.  Well, 215 is sorta closeish.
		//62, we got 62, 124, 248, 496, 558.  Doesn't match up at all.
		//Which means that partial textures can be used.  Lovely.

		//Let's try adding some helper functions so this can be refactored later into a more general-purpose popup popper
		int vOffset = 0;
		drawRowOfPopup(image, vOffset, width, topLeftPopup, topCenterPopup, topRightPopup);
		vOffset+=VTILE_SIZE;
		for (;vOffset < height - VTILE_SIZE; vOffset += VTILE_SIZE) {
			drawRowOfPopup(image, vOffset, width, middleLeftPopup, middleCenterPopup, middleRightPopup);
		}
		vOffset = height - VTILE_SIZE;
		drawRowOfPopup(image, vOffset, width, bottomLeftPopup, bottomCenterPopup, bottomRightPopup);

		ImageTexture texture = new ImageTexture();
		texture.CreateFromImage(image);

		TextureRect rect = new TextureRect();
		rect.Texture = texture;

		return rect;
	}

	private static void drawRowOfPopup(Image image, int vOffset, int width, Image left, Image center, Image right)
	{

		image.BlitRect(left, new Rect2(new Vector2(0, 0), new Vector2(left.GetWidth(), left.GetHeight())), new Vector2(0, vOffset));

		int leftOffset = HTILE_SIZE;
		for (;leftOffset < width - HTILE_SIZE; leftOffset += HTILE_SIZE)
		{
			image.BlitRect(center, new Rect2(new Vector2(0, 0), new Vector2(center.GetWidth(), center.GetHeight())), new Vector2(leftOffset, vOffset));
		}

		leftOffset = width - HTILE_SIZE;
		image.BlitRect(right, new Rect2(new Vector2(0, 0), new Vector2(right.GetWidth(), right.GetHeight())), new Vector2(leftOffset, vOffset));
	}
	
	public static void AddHeaderToPopup(TextureRect thePopup, string headerText, int vOffset)
	{
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
		advisorType.Text = headerText;
		header.AddChild(advisorType);
		header.SetPosition(new Vector2(0, vOffset));
		header.AnchorLeft = 0.0f;
		header.AnchorRight = 1.0f;
		header.MarginRight = 10;    //For some reason this isn't causing it to be indented 10 pixels from the right.
		thePopup.AddChild(header);
	}

	/**
	 * N.B. Some popups should react to certain keys, e.g. the Build City popup should close without building if you
	 * press escape.  Those popups will have to implement this functionality.
	 *
	 * If we find that the majority of popups should close on Escape, we may want to make that the default,
	 * but so far, 2 out of 3 popups do not close on escape.
	 **/
	public override void _UnhandledInput(InputEvent @event)
	{
		if (this.Visible) {
			if (@event is InputEventKey eventKey)
			{
				//As I've added more shortcuts, I've realized checking all of them here could be irksome.
				//For now, I'm thinking it would make more sense to process or allow through the ones that should go through,
				//as most of the global ones should *not* go through here.
				if (eventKey.Pressed)
				{
					GetTree().SetInputAsHandled();
				}
			}
		}
	}

	public static void AddButton(TextureRect thePopup, string label, int verticalPosition, string actionName)
	{
		const int HORIZONTAL_POSITION = 30;
		TextureButton newButton = new TextureButton();
		newButton.TextureNormal = InactiveButton;
		newButton.TextureHover = HoverButton;
		newButton.SetPosition(new Vector2(HORIZONTAL_POSITION, verticalPosition));
		thePopup.AddChild(newButton);
		newButton.Connect("pressed", thePopup, actionName);
				
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
		thePopup.AddChild(newButtonLabel);
		newButtonLabel.Connect("pressed", thePopup, actionName);
	}
}
