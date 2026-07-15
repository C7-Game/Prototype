using Godot;
using ConvertCiv3Media;
using System;
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

	private const int HTILE_SIZE = 61;
	private const int VTILE_SIZE = 44;

	private static ImageTexture? _ninePatchTexture = null;

	public const int DARK_PATCH_SIZE = 8;
	private static ImageTexture? _darkEdgeTexture = null;

	internal static ImageTexture? EnsureDarkEdgeTexture() {
		if (_darkEdgeTexture != null) return _darkEdgeTexture;
		int total = DARK_PATCH_SIZE * 3; // 24x24

		Image img = Image.Create(total, total, false, Image.Format.Rgba8);

		Color edgeColor = new(0, 0, 0, 0.7f);
		Color cornerColor = new(0, 0, 0, 0.9f); // darker corners

		for (int x = 0; x < total; x++) {
			for (int y = 0; y < total; y++) {
				bool edge = x < DARK_PATCH_SIZE || x >= total - DARK_PATCH_SIZE
						 || y < DARK_PATCH_SIZE || y >= total - DARK_PATCH_SIZE;
				bool corner = (x < DARK_PATCH_SIZE || x >= total - DARK_PATCH_SIZE)
						   && (y < DARK_PATCH_SIZE || y >= total - DARK_PATCH_SIZE);
				if (corner) img.SetPixel(x, y, cornerColor);
				else if (edge) img.SetPixel(x, y, edgeColor);
			}
		}

		_darkEdgeTexture = ImageTexture.CreateFromImage(img);
		return _darkEdgeTexture;
	}

	private static ImageTexture EnsureNinePatchTexture() {
		if (_ninePatchTexture != null) return _ninePatchTexture;

		Image topLeftPopup = TextureLoader.Load("popup_background.top_left").GetImage();
		Image topCenterPopup = TextureLoader.Load("popup_background.top_center").GetImage();
		Image topRightPopup = TextureLoader.Load("popup_background.top_right").GetImage();
		Image middleLeftPopup = TextureLoader.Load("popup_background.middle_left").GetImage();
		Image middleCenterPopup = TextureLoader.Load("popup_background.middle_center").GetImage();
		Image middleRightPopup = TextureLoader.Load("popup_background.middle_right").GetImage();
		Image bottomLeftPopup = TextureLoader.Load("popup_background.bottom_left").GetImage();
		Image bottomCenterPopup = TextureLoader.Load("popup_background.bottom_center").GetImage();
		Image bottomRightPopup = TextureLoader.Load("popup_background.bottom_right").GetImage();

		int compW = topLeftPopup.GetWidth() + topCenterPopup.GetWidth() + topRightPopup.GetWidth();
		int compH = topLeftPopup.GetHeight() + middleCenterPopup.GetHeight() + bottomLeftPopup.GetHeight();

		Image composite = Image.Create(compW, compH, false, Image.Format.Rgba8);
		composite.Fill(Color.Color8(0, 0, 0, 0));

		// Row 0
		composite.BlitRect(topLeftPopup, new Rect2I(new Vector2I(0, 0), new Vector2I(topLeftPopup.GetWidth(), topLeftPopup.GetHeight())), new Vector2I(0, 0));
		composite.BlitRect(topCenterPopup, new Rect2I(new Vector2I(0, 0), new Vector2I(topCenterPopup.GetWidth(), topCenterPopup.GetHeight())), new Vector2I(HTILE_SIZE, 0));
		composite.BlitRect(topRightPopup, new Rect2I(new Vector2I(0, 0), new Vector2I(topRightPopup.GetWidth(), topRightPopup.GetHeight())), new Vector2I(HTILE_SIZE * 2, 0));

		// Row 1
		composite.BlitRect(middleLeftPopup, new Rect2I(new Vector2I(0, 0), new Vector2I(middleLeftPopup.GetWidth(), middleLeftPopup.GetHeight())), new Vector2I(0, VTILE_SIZE));
		composite.BlitRect(middleCenterPopup, new Rect2I(new Vector2I(0, 0), new Vector2I(middleCenterPopup.GetWidth(), middleCenterPopup.GetHeight())), new Vector2I(HTILE_SIZE, VTILE_SIZE));
		composite.BlitRect(middleRightPopup, new Rect2I(new Vector2I(0, 0), new Vector2I(middleRightPopup.GetWidth(), middleRightPopup.GetHeight())), new Vector2I(HTILE_SIZE * 2, VTILE_SIZE));

		// Row 2
		composite.BlitRect(bottomLeftPopup, new Rect2I(new Vector2I(0, 0), new Vector2I(bottomLeftPopup.GetWidth(), bottomLeftPopup.GetHeight())), new Vector2I(0, VTILE_SIZE * 2));
		composite.BlitRect(bottomCenterPopup, new Rect2I(new Vector2I(0, 0), new Vector2I(bottomCenterPopup.GetWidth(), bottomCenterPopup.GetHeight())), new Vector2I(HTILE_SIZE, VTILE_SIZE * 2));
		composite.BlitRect(bottomRightPopup, new Rect2I(new Vector2I(0, 0), new Vector2I(bottomRightPopup.GetWidth(), bottomRightPopup.GetHeight())), new Vector2I(HTILE_SIZE * 2, VTILE_SIZE * 2));

		_ninePatchTexture = ImageTexture.CreateFromImage(composite);
		return _ninePatchTexture;
	}

	protected void AddButton(string label, int verticalPosition, System.Action action) {
		const int HORIZONTAL_POSITION = 30;

		Civ3MenuButton button = new() {
			Text = label,
			FontSize = 14,
		};
		button.SetPosition(new Vector2(HORIZONTAL_POSITION, verticalPosition));
		AddChild(button);
		button.Pressed += action;
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

	protected void AddTexture(int width, int height) {
		Image image = Image.Create(width, height, false, Image.Format.Rgba8);
		image.Fill(Color.Color8(0, 0, 0, 0));
		this.Texture = ImageTexture.CreateFromImage(image);
	}

	protected void AddBackground(int width, int height, int vOffset = 0) {
		Control background = CreateBackground(width, height);
		background.SetPosition(new Vector2(0, vOffset));
		AddChild(background);
	}

	private Control CreateBackground(int width, int height) {
		NinePatchRect patch = new();
		patch.Texture = EnsureNinePatchTexture()!;
		patch.PatchMarginLeft = HTILE_SIZE;
		patch.PatchMarginRight = HTILE_SIZE;
		patch.PatchMarginTop = VTILE_SIZE;
		patch.PatchMarginBottom = VTILE_SIZE;
		patch.SetSize(new Vector2(width, height));
		return patch;
	}

	protected void AddConfirmButton(Vector2 position, System.Action action) {
		ImageTexture circleTexture= TextureLoader.Load("ui.confirm.normal");
		ImageTexture circleHover = TextureLoader.Load("ui.confirm.hover");
		ImageTexture circlePressed = TextureLoader.Load("ui.confirm.pressed");
		TextureButton confirmButton = new TextureButton();
		confirmButton.TextureNormal = circleTexture;
		confirmButton.TextureHover = circleHover;
		confirmButton.TexturePressed = circlePressed;
		confirmButton.SetPosition(position);
		confirmButton.Pressed += action;
		AddChild(confirmButton);
	}

	protected void AddCancelButton(Vector2 position) {
		ImageTexture xTexture = TextureLoader.Load("ui.cancel.normal");
		ImageTexture xHover = TextureLoader.Load("ui.cancel.hover");
		ImageTexture xPressed = TextureLoader.Load("ui.cancel.pressed");
		TextureButton cancelButton = new TextureButton();
		cancelButton.TextureNormal = xTexture;
		cancelButton.TextureHover = xHover;
		cancelButton.TexturePressed = xPressed;
		cancelButton.SetPosition(position);
		cancelButton.Pressed += GetParent<PopupOverlay>().OnHidePopup;
		AddChild(cancelButton);
	}
}
