using System;
using C7GameData;
using ConvertCiv3Media;
using Godot;
using Serilog;
using Serilog.Events;

namespace C7.Map {
	public partial class CityLabelScene : Node2D {
		private ILogger log = LogManager.ForContext<CityLabelScene>();

		private City city;
		private Tile tile;

		private ImageTexture cityTexture;

		const int CITY_LABEL_HEIGHT = 23;
		const int LEFT_RIGHT_BOXES_WIDTH = 24;
		const int LEFT_RIGHT_BOXES_HEIGHT = CITY_LABEL_HEIGHT - 2;
		const int TEXT_ROW_HEIGHT = 9;

		ImageTexture cityLabel = new ImageTexture();

		private TextureRect labelTextureRect = new TextureRect();
		Label cityNameLabel = new Label();
		Label productionLabel = new Label();
		Label popSizeLabel = new Label();

		private static FontFile smallFont = new FontFile();
		private static FontFile midSizedFont = new FontFile();

		private static Pcx cityIcons = Util.LoadPCX("Art/Cities/city icons.pcx");
		private static Image nonEmbassyStar;
		private static Theme smallFontTheme = new Theme();
		private static Theme popThemeRed = new Theme();
		private static Theme popSizeTheme = new Theme();

		private int lastLabelWidth = 0;
		private int lastTurnsUntilGrowth;
		private int lastTurnsUntilProductFinished;
		private int lastCitySize;
		private string lastProductionName;
		private bool wasCapital;

		static CityLabelScene() {
			smallFontTheme.DefaultFont = smallFont;
			smallFontTheme.SetColor("font_color", "Label", Color.Color8(255, 255, 255, 255));
			smallFontTheme.SetFontSize("font_size", "Label", 11);
			popSizeTheme.DefaultFont = midSizedFont;
			popSizeTheme.SetColor("font_color", "Label", Color.Color8(255, 255, 255, 255));
			popSizeTheme.SetFontSize("font_size", "Label", 18);
			popThemeRed.DefaultFont = midSizedFont;
			popThemeRed.SetColor("font_color", "Label", Color.Color8(255, 255, 255, 255));
			popThemeRed.SetFontSize("font_size", "Label", 18);

			//Mid-Size font skips the cache as it sets a custom size
			midSizedFont = ResourceLoader.Load<FontFile>("res://Fonts/NotoSans-Regular.ttf");

			//Small font doesn't, because otherwise it makes everything small
			smallFont = ResourceLoader.Load<FontFile>("res://Fonts/NotoSans-Regular.ttf", null, ResourceLoader.CacheMode.Ignore);
			//Must set the FixedSize so Godot can calculate the width of the font for city labels
			smallFont.FixedSize = 11;

			nonEmbassyStar = PCXToGodot.getImageFromPCX(cityIcons, 20, 1, 18, 18);
		}

		public CityLabelScene(City city, Tile tile) {
			this.city = city;
			this.tile = tile;

			labelTextureRect.MouseFilter = Control.MouseFilterEnum.Ignore;
			cityNameLabel.MouseFilter = Control.MouseFilterEnum.Ignore;
			productionLabel.MouseFilter = Control.MouseFilterEnum.Ignore;
			popSizeLabel.MouseFilter = Control.MouseFilterEnum.Ignore;

			cityNameLabel.Text = city.name;
			AddChild(labelTextureRect);
			AddChild(cityNameLabel);
			AddChild(productionLabel);
			AddChild(popSizeLabel);

			cityNameLabel.Theme = smallFontTheme;
			cityNameLabel.OffsetTop = 24;

			productionLabel.Theme = smallFontTheme;
			productionLabel.OffsetTop = 36;

			popSizeLabel.Theme = popSizeTheme;
			popSizeLabel.OffsetTop = 28;

			lastTurnsUntilGrowth = city.TurnsUntilGrowth();
			lastTurnsUntilProductFinished = city.TurnsUntilProductionFinished();
			lastCitySize = city.size;
			lastProductionName = city.itemBeingProduced.name;
			wasCapital = city.IsCapital();

			resize(lastTurnsUntilGrowth, lastTurnsUntilProductFinished, lastCitySize, lastProductionName, wasCapital);
		}

		private string getCityNameAndGrowthString(int turnsUntilGrowth) {
			string turnsUntilGrowthText = turnsUntilGrowth == int.MaxValue || turnsUntilGrowth < 0 ? "- -" : turnsUntilGrowth.ToString();
			return $"{city.name} : {turnsUntilGrowthText}";
		}

		private void resize(int turnsUntilGrowth, int turnsUntilProductionFinished, int citySize, string productionName, bool isCapital) {
			string productionDescription = $"{productionName} : {turnsUntilProductionFinished}";
			int productionDescriptionWidth = (int)smallFont.GetStringSize(productionDescription).X;

			string cityNameAndGrowth = getCityNameAndGrowthString(turnsUntilGrowth);
			int cityNameAndGrowthWidth = (int)smallFont.GetStringSize(cityNameAndGrowth).X;
			int maxTextWidth = Math.Max(cityNameAndGrowthWidth, productionDescriptionWidth);
			int cityLabelWidth = maxTextWidth + (isCapital? 70 : 45); //TODO: Is 65 right?  70?  Will depend on whether it's capital, too

			int textAreaWidth = cityLabelWidth - (isCapital ? 50 : 25);
			if (log.IsEnabled(LogEventLevel.Verbose)) {
				log.Verbose("City label width: " + cityLabelWidth);
				log.Verbose("Text area width: " + textAreaWidth);
			}
			if (cityLabelWidth != lastLabelWidth) {
				Image labelBackground = CreateLabelBackground(cityLabelWidth, city, textAreaWidth);
				cityLabel = ImageTexture.CreateFromImage(labelBackground);
				lastLabelWidth = cityLabelWidth;
				labelTextureRect.Texture = cityLabel;
				labelTextureRect.OffsetLeft = (cityLabelWidth / -2);
				labelTextureRect.OffsetTop = 24;
			}
			int cityNameOffset = cityNameAndGrowthWidth / -2 - 8;
			int prodDescriptionOffset = productionDescriptionWidth / -2 - 8;
			if (!isCapital) {
				cityNameOffset += 12;
				prodDescriptionOffset += 12;
			}
			cityNameLabel.Text = cityNameAndGrowth;
			cityNameLabel.OffsetLeft = cityNameOffset;

			productionLabel.Text = productionDescription;
			productionLabel.OffsetLeft = prodDescriptionOffset;

			string popSizeString = citySize.ToString();
			int popSizeWidth = (int)midSizedFont.GetStringSize(popSizeString).X;
			int popSizeOffset = LEFT_RIGHT_BOXES_WIDTH / 2 - popSizeWidth / 2;

			if (turnsUntilGrowth < 0) {
				popSizeLabel.Theme = popThemeRed;
			}

			popSizeLabel.Text = popSizeString;
			popSizeLabel.OffsetLeft = cityLabelWidth / -2 + popSizeOffset;
		}

		public override void _Process(double delta) {
			int turnsUntilGrowth = city.TurnsUntilGrowth();
			int turnsUntilProductionFinished = city.TurnsUntilProductionFinished();
			int citySize = city.size;
			string productionName = city.itemBeingProduced.name;
			bool isCaptial = city.IsCapital();

			if (turnsUntilGrowth != lastTurnsUntilGrowth || turnsUntilProductionFinished != lastTurnsUntilProductFinished || citySize != lastCitySize || productionName != lastProductionName || isCaptial != wasCapital) {
				lastTurnsUntilGrowth = turnsUntilGrowth;
				lastTurnsUntilProductFinished = turnsUntilProductionFinished;
				lastCitySize = citySize;
				lastProductionName = productionName;
				wasCapital = isCaptial;
				resize(turnsUntilGrowth, turnsUntilProductionFinished, citySize, productionName, isCaptial);
			}
		}

		private Image CreateLabelBackground(int cityLabelWidth, City city, int textAreaWidth)
		{
			//Label/name/producing area
			Image labelImage = Image.Create(cityLabelWidth, CITY_LABEL_HEIGHT, false, Image.Format.Rgba8);
			labelImage.Fill(Color.Color8(0, 0, 0, 0));
			byte transparencyLevel = 192; //25%
			Color civColor = new Color(city.owner.color);
			civColor = new Color(civColor, transparencyLevel);
			Color civColorDarker = Color.Color8(0, 0, 138, transparencyLevel); //todo: automate the darker() function.  maybe less transparency?
			Color topRowGrey = Color.Color8(32, 32, 32, transparencyLevel);
			Color bottomRowGrey = Color.Color8(48, 48, 48, transparencyLevel);
			Color backgroundGrey = Color.Color8(64, 64, 64, transparencyLevel);
			Color borderGrey = Color.Color8(80, 80, 80, transparencyLevel);

			Image horizontalBorder = Image.Create(cityLabelWidth - 2, 1, false, Image.Format.Rgba8);
			horizontalBorder.Fill(borderGrey);
			labelImage.BlitRect(horizontalBorder, new Rect2I(0, 0, new Vector2I(cityLabelWidth - 2, 1)), new Vector2I(1, 0));
			labelImage.BlitRect(horizontalBorder, new Rect2I(0, 0, new Vector2I(cityLabelWidth - 2, 1)), new Vector2I(1, 22));

			Image verticalBorder = Image.Create(1, CITY_LABEL_HEIGHT - 2, false, Image.Format.Rgba8);
			verticalBorder.Fill(borderGrey);
			labelImage.BlitRect(verticalBorder, new Rect2I(0, 0, new Vector2I(1, 23)), new Vector2I(0, 1));
			labelImage.BlitRect(verticalBorder, new Rect2I(0, 0, new Vector2I(1, 23)), new Vector2I(cityLabelWidth - 1, 1));

			Image bottomRow = Image.Create(textAreaWidth, 1, false, Image.Format.Rgba8);
			bottomRow.Fill(bottomRowGrey);
			labelImage.BlitRect(bottomRow, new Rect2I(0, 0, new Vector2I(textAreaWidth, 1)), new Vector2I(25, 21));

			Image topRow = Image.Create(textAreaWidth, 1, false, Image.Format.Rgba8);
			topRow.Fill(topRowGrey);
			labelImage.BlitRect(topRow, new Rect2I(0, 0, new Vector2I(textAreaWidth, 1)), new Vector2I(25, 1));

			Image background = Image.Create(textAreaWidth, TEXT_ROW_HEIGHT, false, Image.Format.Rgba8);
			background.Fill(backgroundGrey);
			labelImage.BlitRect(background, new Rect2I(0, 0, new Vector2I(textAreaWidth, 9)), new Vector2I(25, 2));
			labelImage.BlitRect(background, new Rect2I(0, 0, new Vector2I(textAreaWidth, 9)), new Vector2I(25, 12));

			Image centerDivider = Image.Create(textAreaWidth, 1, false, Image.Format.Rgba8);
			centerDivider.Fill(civColor);
			labelImage.BlitRect(centerDivider, new Rect2I(0, 0, new Vector2I(textAreaWidth, 1)), new Vector2I(25, 11));

			Image leftAndRightBoxes = Image.Create(LEFT_RIGHT_BOXES_WIDTH, LEFT_RIGHT_BOXES_HEIGHT, false, Image.Format.Rgba8);
			leftAndRightBoxes.Fill(civColor);
			labelImage.BlitRect(leftAndRightBoxes, new Rect2I(0, 0, new Vector2I(24, 21)), new Vector2I(1, 1));
			if (city.IsCapital()) {
				labelImage.BlitRect(leftAndRightBoxes, new Rect2I(0, 0, new Vector2I(24, 21)), new Vector2I(cityLabelWidth - 25, 1));
				labelImage.BlendRect(nonEmbassyStar, new Rect2I(0, 0, new Vector2I(18, 18)), new Vector2I(cityLabelWidth - 24, 2));
			}
			//todo: darker shades of civ color around edges
			return labelImage;
		}
	}
}
