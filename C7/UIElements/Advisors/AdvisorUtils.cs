using Godot;

public static class AdvisorUtils {
	public static TextureButton CreateExitButton(Control parent, Vector2? position = null) {
		Vector2 buttonPosition = position ?? new Vector2(952, 720);

		TextureButton btn = new();
		TextureLoader.SetButtonTextures(btn, "ui.exit");
		btn.SetPosition(buttonPosition);
		parent.AddChild(btn);
		return btn;
	}

	public static TextureRect CreateAdvisorHead(Control parent, AdvisorHead.Advisor advisor, Vector2? position = null) {
		Vector2 headPosition = position ?? new Vector2(851, 0);

		TextureRect advisorHead = new();
		advisorHead.Texture = AdvisorHead.GetPopupImage(advisor, AdvisorHead.Mood.Happy, eraIndex: 0);
		advisorHead.SetPosition(headPosition);
		parent.AddChild(advisorHead);

		return advisorHead;
	}

	public static (TextureButton box, Label label) CreateAdvisorDialogBox(Control parent, Vector2? position = null) {
		Vector2 boxPosition = position ?? new Vector2(806, 110);
		Vector2 labelPosition = boxPosition + new Vector2(9, 9);

		ImageTexture dialogBoxTexture = TextureLoader.Load("advisors.dialog_box");
		TextureButton dialogBox = new TextureButton();
		dialogBox.TextureNormal = dialogBoxTexture;
		dialogBox.SetPosition(boxPosition);
		parent.AddChild(dialogBox);

		//TODO: Multi-line capabilities
		Label dialogBoxLabel = new();
		dialogBoxLabel.Text = "You are running OpenCiv3!";
		dialogBoxLabel.SetPosition(labelPosition);
		parent.AddChild(dialogBoxLabel);

		return (dialogBox, dialogBoxLabel);
	}

	public static void CreateAdvisorTitle(Control parent, float containerWidth, string advisorTitleString) {
		int bigFontSize = 26;
		// int middleFontSize = 20;
		int bigFontGlyphSpacing = 14;
		int bigFontGlyphSpaceSpacing = 22;

		FontFile regularFont = ResourceLoader.Load<FontFile>("res://Fonts/NotoSans-Regular.ttf");
		Theme regularBigFontTheme = new();
		regularBigFontTheme.DefaultFont = regularFont;
		regularBigFontTheme.SetFontSize("font_size", "Label", bigFontSize);

		FontVariation fontVariation = new FontVariation
		{
			BaseFont = regularFont,
			SpacingGlyph = bigFontGlyphSpacing,
			SpacingSpace = bigFontGlyphSpaceSpacing,
		};

		Theme regularThemeWithCustomSpacing = new Theme();
		regularThemeWithCustomSpacing.SetFont("font", "Label", fontVariation);
		regularThemeWithCustomSpacing.SetFontSize("font_size", "Label", bigFontSize);

		float advisorTitleStringWidth = GetStringSizeWithCustomSpacing(regularFont, advisorTitleString, bigFontSize,
			bigFontGlyphSpacing, bigFontGlyphSpaceSpacing).X;

		float advisorTitleOffsetLeft = (containerWidth / 2.0f) - (advisorTitleStringWidth) / 2.0f;

		Label advisorTitle = new() {
			Text = advisorTitleString,
			OffsetLeft = advisorTitleOffsetLeft,
			OffsetTop = 15,
			Theme = regularThemeWithCustomSpacing,
		};
		parent.AddChild(advisorTitle);
	}

	private static Vector2 GetStringSizeWithCustomSpacing(Font font, string input, int fontSize = 16, int glyphSpacing = 0, int glyphSpaceSpacing = 0) {

		float extraSpacing = 0.0f;
		for (int i = 0; i < input.Length; i++) {
			if (i < input.Length - 1) {
				if (char.IsWhiteSpace(input[i]) && glyphSpaceSpacing > 0) {
					extraSpacing += glyphSpaceSpacing;
				} else {
					extraSpacing += glyphSpacing;
				}
			}
		}

		Vector2 originalSize = font.GetStringSize(input, fontSize: fontSize);
		return new Vector2(originalSize.X + extraSpacing, originalSize.Y);
	}
}
