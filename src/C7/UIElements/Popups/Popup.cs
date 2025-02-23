using Godot;
using ConvertCiv3Media;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using Serilog;

public partial class Margins {
	public Margins(float top = 0, float bottom = 0, float left = 0, float right = 0) {
		this.top = top;
		this.bottom = bottom;
		this.left = left;
		this.right = right;
	}

	public float top;
	public float bottom;
	public float left;
	public float right;
}

/*
 * Popup displays a popup menu or dialog. Margins alignment determines where the popup appears on the screen,
 * and margins can be used to add spacing around the popup. The AddBackground method will add the background
 * texture to the popup, which will be rendered at the position determined by the alignment and margins specified.
 * Additionally, AddBackground accepts an int vOffset parameter which will render the background texture vOffset
 * pixels lower than it would otherwise. This is used when a popup includes an advisor, since the vOffset adds a
 * transparent region above the top of the popup background in which the advisor texture can be drawn.
 */
public partial class Popup : TextureRect {
	private ILogger log = LogManager.ForContext<Popup>();

	public BoxContainer.AlignmentMode alignment;
	public Margins margins;

	const int HTILE_SIZE = 61;
	const int VTILE_SIZE = 44;
	private readonly static int BUTTON_LABEL_OFFSET = 0;    //Necessary in Godot 3, appears unnecessary in 4

	private static ImageTexture InactiveButton = Util.LoadTextureFromPCX("Art/buttonsFINAL.pcx", 1, 1, 20, 20, false);
	private static ImageTexture HoverButton = Util.LoadTextureFromPCX("Art/buttonsFINAL.pcx", 22, 1, 20, 20, false);
	private static Dictionary<(int, int), ImageTexture> backgroundCache = new Dictionary<(int, int), ImageTexture>();

	protected void AddButton(string label, int verticalPosition, Action action) {
		const int HORIZONTAL_POSITION = 30;
		TextureButton newButton = new TextureButton();
		newButton.TextureNormal = InactiveButton;
		newButton.TextureHover = HoverButton;
		newButton.SetPosition(new Vector2(HORIZONTAL_POSITION, verticalPosition));
		AddChild(newButton);
		newButton.Pressed += action;

		Theme theme = new Theme();
		theme.SetFontSize("font_size", "Button", 14);
		Button newButtonLabel = new Button();
		newButtonLabel.Theme = theme;
		newButtonLabel.Text = label;

		newButtonLabel.SetPosition(new Vector2(HORIZONTAL_POSITION + 25, verticalPosition + BUTTON_LABEL_OFFSET));
		AddChild(newButtonLabel);
		newButtonLabel.Pressed += action;
	}

	protected void AddHeader(string text, int vOffset) {
		HBoxContainer header = new HBoxContainer();
		header.Alignment = BoxContainer.AlignmentMode.Center;
		Label advisorType = new Label();

		//Set the font size.  For labels, there is no one-off override, so we have to
		//set it on a theme like this.
		//The SetFont arguments aren't documented in a way that a non-Godot expert can understand
		//My current understanding is that we need to set the first parameter to "font", and the
		//second to whatever type it should apply to.  But that is based on nothing official.
		//Also you can set the size with bigFont.Size = 72, but that applies everywhere the font
		//is used in the whole program.  Not recommended.
		FontFile bigFont = ResourceLoader.Load<FontFile>("res://Fonts/NSansFont24Pt.tres");
		Theme theme = new Theme();
		theme.SetFont("font", "Label", bigFont);
		theme.SetFontSize("font_size", "Label", 24);
		advisorType.Theme = theme;
		advisorType.Text = text;
		header.AddChild(advisorType);
		header.SetPosition(new Vector2(0, vOffset));
		header.AnchorLeft = 0.0f;
		header.AnchorRight = 1.0f;
		header.OffsetRight = 10;    // For some reason this isn't causing it to be indented 10 pixels from the right.
		AddChild(header);
	}

	private void DrawRow(Image image, int vOffset, int width, Image left, Image center, Image right) {

		image.BlitRect(left, new Rect2I(new Vector2I(0, 0), new Vector2I(left.GetWidth(), left.GetHeight())), new Vector2I(0, vOffset));

		int leftOffset = HTILE_SIZE;
		for (; leftOffset < width - HTILE_SIZE; leftOffset += HTILE_SIZE) {
			image.BlitRect(center, new Rect2I(new Vector2I(0, 0), new Vector2I(center.GetWidth(), center.GetHeight())), new Vector2I(leftOffset, vOffset));
		}

		leftOffset = width - HTILE_SIZE;
		image.BlitRect(right, new Rect2I(new Vector2I(0, 0), new Vector2I(right.GetWidth(), right.GetHeight())), new Vector2I(leftOffset, vOffset));
	}

	protected void AddTexture(int width, int height) {
		Image image = Image.Create(width, height, false, Image.Format.Rgba8);
		image.Fill(Color.Color8(0, 0, 0, 0));
		this.Texture = ImageTexture.CreateFromImage(image);
	}

	protected void AddBackground(int width, int height, int vOffset = 0) {
		TextureRect background = CreateBackground(width, height);
		background.SetPosition(new Vector2(0, vOffset));
		AddChild(background);
	}

	private TextureRect CreateBackground(int width, int height) {
		TextureRect rect = new TextureRect();

		if (backgroundCache.ContainsKey((width, height))) {
			rect.Texture = backgroundCache[(width, height)];
			return rect;
		}

		Image image = Image.Create(width, height, false, Image.Format.Rgba8);

		Pcx popupborders = Util.LoadPCX("Art/popupborders.pcx");

		//The pop-up part is the tricky part
		Stopwatch imageTimer = new Stopwatch();
		imageTimer.Start();
		Image topLeftPopup = PCXToGodot.getImageFromPCX(popupborders, 251, 1, 61, 44);
		Image topCenterPopup = PCXToGodot.getImageFromPCX(popupborders, 313, 1, 61, 44);
		Image topRightPopup = PCXToGodot.getImageFromPCX(popupborders, 375, 1, 61, 44);
		Image middleLeftPopup = PCXToGodot.getImageFromPCX(popupborders, 251, 46, 61, 44);
		Image middleCenterPopup = PCXToGodot.getImageFromPCX(popupborders, 313, 46, 61, 44);
		Image middleRightPopup = PCXToGodot.getImageFromPCX(popupborders, 375, 46, 61, 44);
		Image bottomLeftPopup = PCXToGodot.getImageFromPCX(popupborders, 251, 91, 61, 44);
		Image bottomCenterPopup = PCXToGodot.getImageFromPCX(popupborders, 313, 91, 61, 44);
		Image bottomRightPopup = PCXToGodot.getImageFromPCX(popupborders, 375, 91, 61, 44);
		imageTimer.Stop();
		TimeSpan stopwatchElapsed = imageTimer.Elapsed;
		log.Debug("Image creation time: " + Convert.ToInt32(stopwatchElapsed.TotalMilliseconds) + " ms");

		//Dimensions are 530x320.  The leaderhead takes up 110.  So the popup is 530x210.
		//We have multiples of... 62? For the horizontal dimension, 45 for vertical.
		//45 does not fit into 210.  90, 135, 180, 215.  Well, 215 is sorta closeish.
		//62, we got 62, 124, 248, 496, 558.  Doesn't match up at all.
		//Which means that partial textures can be used.  Lovely.

		//Let's try adding some helper functions so this can be refactored later into a more general-purpose popup popper
		int vOffset = 0;
		DrawRow(image, vOffset, width, topLeftPopup, topCenterPopup, topRightPopup);
		vOffset += VTILE_SIZE;
		for (; vOffset < height - VTILE_SIZE; vOffset += VTILE_SIZE) {
			DrawRow(image, vOffset, width, middleLeftPopup, middleCenterPopup, middleRightPopup);
		}
		vOffset = height - VTILE_SIZE;
		DrawRow(image, vOffset, width, bottomLeftPopup, bottomCenterPopup, bottomRightPopup);

		ImageTexture texture = ImageTexture.CreateFromImage(image);
		backgroundCache.Add((width, height), texture);

		rect.Texture = texture;
		return rect;
	}

}
