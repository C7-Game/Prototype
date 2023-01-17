using System;
using C7GameData;
using ConvertCiv3Media;
using Godot;
using Serilog;
using Serilog.Events;

namespace C7.Map {
	public class CityScene : Node2D {
		private ILogger log = LogManager.ForContext<CityScene>();

		private City city;
		private Tile tile;
		private Vector2 tileCenter;

		private Vector2 citySpriteSize;

		private ImageTexture cityTexture;

		private DynamicFont smallFont = new DynamicFont();
		private DynamicFont midSizedFont = new DynamicFont();

		private Pcx cityIcons = Util.LoadPCX("Art/Cities/city icons.pcx");
		private Image nonEmbassyStar;

		const int CITY_LABEL_HEIGHT = 23;
		const int LEFT_RIGHT_BOXES_WIDTH = 24;
		const int LEFT_RIGHT_BOXES_HEIGHT = CITY_LABEL_HEIGHT - 2;
		const int TEXT_ROW_HEIGHT = 9;

		ImageTexture cityLabel = new ImageTexture();

		private TextureRect theTexture = new TextureRect();
		private TextureRect labelTextureRect = new TextureRect();
		Label cityNameLabel = new Label();
		Label productionLabel = new Label();
		Label popSizeLabel = new Label();

		Theme smallFontTheme = new Theme();
		Theme popThemeRed = new Theme();
		Theme popSizeTheme = new Theme();

		public CityScene(City city, Tile tile, Vector2 tileCenter) {
			this.city = city;
			this.tile = tile;
			this.tileCenter = tileCenter;

			smallFontTheme.DefaultFont = smallFont;
			smallFontTheme.SetColor("font_color", "Label", Color.Color8(255, 255, 255, 255));
			popSizeTheme.DefaultFont = midSizedFont;
			popSizeTheme.SetColor("font_color", "Label", Color.Color8(255, 255, 255, 255));
			popThemeRed.DefaultFont = midSizedFont;
			popThemeRed.SetColor("font_color", "Label", Color.Color8(255, 255, 255, 255));

			//TODO: Generalize, support multiple city types, etc.
			Pcx pcx = Util.LoadPCX("Art/Cities/rMIDEAST.PCX");
			int height = pcx.Height/4;
			int width = pcx.Width/3;
			cityTexture = Util.LoadTextureFromPCX("Art/Cities/rMIDEAST.PCX", 0, 0, width, height);
			citySpriteSize = new Vector2(width, height);


			smallFont.FontData = ResourceLoader.Load<DynamicFontData>("res://Fonts/NotoSans-Regular.ttf");
			smallFont.Size = 11;

			midSizedFont.FontData = ResourceLoader.Load<DynamicFontData>("res://Fonts/NotoSans-Regular.ttf");
			midSizedFont.Size = 18;

			nonEmbassyStar = PCXToGodot.getImageFromPCX(cityIcons, 20, 1, 18, 18);
			AddChild(theTexture);
			AddChild(labelTextureRect);
			AddChild(cityNameLabel);
			AddChild(productionLabel);
			AddChild(popSizeLabel);
		}

		public override void _Draw() {
			base._Draw();

			theTexture.MarginLeft = tileCenter.x - (float)0.5 * citySpriteSize.x;
			theTexture.MarginTop = tileCenter.y - (float)0.5 * citySpriteSize.y;
			theTexture.Texture = cityTexture;

			int turnsUntilGrowth = city.TurnsUntilGrowth();
			string turnsUntilGrowthText = turnsUntilGrowth == int.MaxValue || turnsUntilGrowth < 0 ? "- -" : "" + turnsUntilGrowth;
			string cityNameAndGrowth = $"{city.name} : {turnsUntilGrowthText}";
			string productionDescription = city.itemBeingProduced.name + " : " + city.TurnsUntilProductionFinished();

			int cityNameAndGrowthWidth = (int)smallFont.GetStringSize(cityNameAndGrowth).x;
			int productionDescriptionWidth = (int)smallFont.GetStringSize(productionDescription).x;
			int maxTextWidth = Math.Max(cityNameAndGrowthWidth, productionDescriptionWidth);

			int cityLabelWidth = maxTextWidth + (city.IsCapital()? 70 : 45);	//TODO: Is 65 right?  70?  Will depend on whether it's capital, too
			int textAreaWidth = cityLabelWidth - (city.IsCapital() ? 50 : 25);
			if (log.IsEnabled(LogEventLevel.Verbose)) {
				log.Verbose("Width of city name = " + maxTextWidth);
				log.Verbose("City label width: " + cityLabelWidth);
				log.Verbose("Text area width: " + textAreaWidth);
			}

			Image labelBackground = CreateLabelBackground(cityLabelWidth, city, textAreaWidth);
			cityLabel.CreateFromImage(labelBackground, 0);

			DrawLabelOnScreen(tileCenter, cityLabelWidth, city, cityLabel);
			DrawTextOnLabel(tileCenter, cityNameAndGrowthWidth, productionDescriptionWidth, city, cityNameAndGrowth, productionDescription, cityLabelWidth);
		}

		private void DrawLabelOnScreen(Vector2 tileCenter, int cityLabelWidth, City city, ImageTexture cityLabel)
		{
			labelTextureRect.MarginLeft = tileCenter.x + (cityLabelWidth / -2);
			labelTextureRect.MarginTop = tileCenter.y + 24;
			labelTextureRect.Texture = cityLabel;
		}

		private void DrawTextOnLabel(Vector2 tileCenter, int cityNameAndGrowthWidth, int productionDescriptionWidth, City city, string cityNameAndGrowth, string productionDescription, int cityLabelWidth) {

			//Destination for font is based on lower-left of baseline of font, not upper left as for blitted rectangles
			int cityNameOffset = cityNameAndGrowthWidth / -2;
			int prodDescriptionOffset = productionDescriptionWidth / -2;
			if (!city.IsCapital()) {
				cityNameOffset += 12;
				prodDescriptionOffset += 12;
			}

			cityNameLabel.Theme = smallFontTheme;
			cityNameLabel.Text = cityNameAndGrowth;
			cityNameLabel.MarginLeft = tileCenter.x + cityNameOffset;
			cityNameLabel.MarginTop = tileCenter.y + 22;

			productionLabel.Theme = smallFontTheme;
			productionLabel.Text = productionDescription;
			productionLabel.MarginLeft = tileCenter.x + prodDescriptionOffset;
			productionLabel.MarginTop = tileCenter.y + 32;

			//City pop size
			string popSizeString = "" + city.size;
			int popSizeWidth = (int)midSizedFont.GetStringSize(popSizeString).x;
			int popSizeOffset = LEFT_RIGHT_BOXES_WIDTH / 2 - popSizeWidth / 2;
			Vector2 popSizeDestination = new Vector2(tileCenter + new Vector2(cityLabelWidth / -2, 24) + new Vector2(popSizeOffset, 18));
			Color popColor = Color.Color8(255, 255, 255, 255);
			if (city.TurnsUntilGrowth() < 0) {
				popColor = Color.Color8(255, 0, 0, 255);
			}

			popSizeLabel.Theme = popSizeTheme;

			if (city.TurnsUntilGrowth() < 0) {
				popSizeLabel.Theme = popThemeRed;
			}

			popSizeLabel.Text = popSizeString;
			popSizeLabel.MarginLeft = tileCenter.x + cityLabelWidth / -2 + popSizeOffset;
			popSizeLabel.MarginTop = tileCenter.y + 22;
		}

		private Image CreateLabelBackground(int cityLabelWidth, City city, int textAreaWidth)
		{
			//Label/name/producing area
			Image labelImage = new Image();
			labelImage.Create(cityLabelWidth, CITY_LABEL_HEIGHT, false, Image.Format.Rgba8);
			labelImage.Fill(Color.Color8(0, 0, 0, 0));
			byte transparencyLevel = 192; //25%
			Color civColor = new Color(city.owner.color);
			civColor = new Color(civColor, transparencyLevel);
			Color civColorDarker = Color.Color8(0, 0, 138, transparencyLevel); //todo: automate the darker() function.  maybe less transparency?
			Color topRowGrey = Color.Color8(32, 32, 32, transparencyLevel);
			Color bottomRowGrey = Color.Color8(48, 48, 48, transparencyLevel);
			Color backgroundGrey = Color.Color8(64, 64, 64, transparencyLevel);
			Color borderGrey = Color.Color8(80, 80, 80, transparencyLevel);

			Image horizontalBorder = new Image();
			horizontalBorder.Create(cityLabelWidth - 2, 1, false, Image.Format.Rgba8);
			horizontalBorder.Fill(borderGrey);
			labelImage.BlitRect(horizontalBorder, new Rect2(0, 0, new Vector2(cityLabelWidth - 2, 1)), new Vector2(1, 0));
			labelImage.BlitRect(horizontalBorder, new Rect2(0, 0, new Vector2(cityLabelWidth - 2, 1)), new Vector2(1, 22));

			Image verticalBorder = new Image();
			verticalBorder.Create(1, CITY_LABEL_HEIGHT - 2, false, Image.Format.Rgba8);
			verticalBorder.Fill(borderGrey);
			labelImage.BlitRect(verticalBorder, new Rect2(0, 0, new Vector2(1, 23)), new Vector2(0, 1));
			labelImage.BlitRect(verticalBorder, new Rect2(0, 0, new Vector2(1, 23)), new Vector2(cityLabelWidth - 1, 1));

			Image bottomRow = new Image();
			bottomRow.Create(textAreaWidth, 1, false, Image.Format.Rgba8);
			bottomRow.Fill(bottomRowGrey);
			labelImage.BlitRect(bottomRow, new Rect2(0, 0, new Vector2(textAreaWidth, 1)), new Vector2(25, 21));

			Image topRow = new Image();
			topRow.Create(textAreaWidth, 1, false, Image.Format.Rgba8);
			topRow.Fill(topRowGrey);
			labelImage.BlitRect(topRow, new Rect2(0, 0, new Vector2(textAreaWidth, 1)), new Vector2(25, 1));

			Image background = new Image();
			background.Create(textAreaWidth, TEXT_ROW_HEIGHT, false, Image.Format.Rgba8);
			background.Fill(backgroundGrey);
			labelImage.BlitRect(background, new Rect2(0, 0, new Vector2(textAreaWidth, 9)), new Vector2(25, 2));
			labelImage.BlitRect(background, new Rect2(0, 0, new Vector2(textAreaWidth, 9)), new Vector2(25, 12));

			Image centerDivider = new Image();
			centerDivider.Create(textAreaWidth, 1, false, Image.Format.Rgba8);
			centerDivider.Fill(civColor);
			labelImage.BlitRect(centerDivider, new Rect2(0, 0, new Vector2(textAreaWidth, 1)), new Vector2(25, 11));

			Image leftAndRightBoxes = new Image();
			leftAndRightBoxes.Create(LEFT_RIGHT_BOXES_WIDTH, LEFT_RIGHT_BOXES_HEIGHT, false, Image.Format.Rgba8);
			leftAndRightBoxes.Fill(civColor);
			labelImage.BlitRect(leftAndRightBoxes, new Rect2(0, 0, new Vector2(24, 21)), new Vector2(1, 1));
			if (city.IsCapital()) {
				labelImage.BlitRect(leftAndRightBoxes, new Rect2(0, 0, new Vector2(24, 21)), new Vector2(cityLabelWidth - 25, 1));
				labelImage.BlendRect(nonEmbassyStar, new Rect2(0, 0, new Vector2(18, 18)), new Vector2(cityLabelWidth - 24, 2));
			}
			//todo: darker shades of civ color around edges
			return labelImage;
		}
	}
}
