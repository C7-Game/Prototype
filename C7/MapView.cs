using System.Collections.Generic;
using System;
using Godot;
using ConvertCiv3Media;
using C7GameData;
using C7Engine;

// Loose layers are for drawing free-form objects on the map. Unlike TileLayers (TODO: pull that out from MapView) they are not restricted to drawing
// tile sprites from a TileSet at fixed locations. They can draw anything, anywhere, although drawing is still tied to tiles for visibility
// purposes. The MapView contains a list of loose layers, each one an object that implements ILooseLayer. Right now to add a new layer you must modify
// the MapView constructor to add it to the list, but (TODO) eventually that will be made moddable.
public abstract class LooseLayer {
	// drawObject draws the things this layer is supposed to draw that are associated with the given tile. Its parameters are:
	//   looseView: The Node2D to actually draw to, e.g., use looseView.DrawCircle(...) to draw a circle. This object also contains a reference to
	//     the MapView in case you need it.
	//   tile: The game tile whose contents are to be drawn. This function gets called for each tile in view of the camera and none out of
	//     view. The same tile may be drawn multiple times at different locations due to edge wrapping.
	//   tileCenter: The location to draw to. You should draw around this location without adjusting for the camera location or zoom since the
	//     MapView already transforms the looseView node to account for those things.
	public abstract void drawObject(LooseView looseView, Tile tile, Vector2 tileCenter);

	// The layer will be skipping during map drawing if visible is false
	public bool visible = true;
}

public class TerrainLayer : LooseLayer {

	public static readonly Vector2 terrainSpriteSize = new Vector2(128, 64);

	// A triple sheet is a sprite sheet containing sprites for three different terrain types including transitions between.
	private List<ImageTexture> tripleSheets;

	public TerrainLayer()
	{
		tripleSheets = loadTerrainTripleSheets();
	}

	public List<ImageTexture> loadTerrainTripleSheets()
	{
		var fileNames = new List<string> {
			"Art/Terrain/xtgc.pcx",
			"Art/Terrain/xpgc.pcx",
			"Art/Terrain/xdgc.pcx",
			"Art/Terrain/xdpc.pcx",
			"Art/Terrain/xdgp.pcx",
			"Art/Terrain/xggc.pcx",
			"Art/Terrain/wCSO.pcx",
			"Art/Terrain/wSSS.pcx",
			"Art/Terrain/wOOO.pcx",
		};
		var tr = new List<ImageTexture>();
		foreach (var fileName in fileNames)
			tr.Add(Util.LoadTextureFromPCX(fileName));
		return tr;
	}

	public override void drawObject(LooseView looseView, Tile tile, Vector2 tileCenter)
	{
		int xSheet = tile.ExtraInfo.BaseTerrainImageID % 9, ySheet = tile.ExtraInfo.BaseTerrainImageID / 9;
		var texRect = new Rect2(new Vector2(xSheet, ySheet) * terrainSpriteSize, terrainSpriteSize);;
		var terrainOffset = new Vector2(0, -1 * MapView.cellSize.y);
		var screenRect = new Rect2(tileCenter - (float)0.5 * terrainSpriteSize + terrainOffset, terrainSpriteSize);
		looseView.DrawTextureRectRegion(tripleSheets[tile.ExtraInfo.BaseTerrainFileID], screenRect, texRect);
	}
}

public class GridLayer : LooseLayer {
	public Color color = Color.Color8(50, 50, 50, 150);
	public float lineWidth = (float)1.0;

	public GridLayer() {}

	public override void drawObject(LooseView looseView, Tile tile, Vector2 tileCenter)
	{
		var cS = MapView.cellSize;
		var left  = tileCenter + new Vector2(-cS.x,  0   );
		var top   = tileCenter + new Vector2( 0   , -cS.y);
		var right = tileCenter + new Vector2( cS.x,  0   );
		looseView.DrawLine(left, top  , color, lineWidth);
		looseView.DrawLine(top , right, color, lineWidth);
	}
}

public class UnitLayer : LooseLayer {
	private ImageTexture unitIcons;
	private int unitIconsWidth;
	private ImageTexture unitMovementIndicators;

	public UnitLayer()
	{
		var iconPCX = new Pcx(Util.Civ3MediaPath("Art/Units/units_32.pcx"));
		unitIcons = PCXToGodot.getImageTextureFromPCX(iconPCX);
		unitIconsWidth = (unitIcons.GetWidth() - 1) / 33;

		var moveIndPCX = new Pcx(Util.Civ3MediaPath("Art/interface/MovementLED.pcx"));
		unitMovementIndicators = PCXToGodot.getImageTextureFromPCX(moveIndPCX);
	}

	public Color getHPColor(float fractionRemaining)
	{
		if (fractionRemaining >= (float)0.67)
			return Color.Color8(0, 255, 0);
		else if (fractionRemaining >= (float)0.34)
			return Color.Color8(255, 255, 0);
		else
			return Color.Color8(255, 0, 0);
	}

	public override void drawObject(LooseView looseView, Tile tile, Vector2 tileCenter)
	{
		if (tile.unitsOnTile.Count == 0)
			return;

		var white = Color.Color8(255, 255, 255);

		// Find unit to draw. If the currently selected unit is on this tile, use that one (also draw a yellow circle behind
		// it). Otherwise, use the top defender.
		MapUnit selectedUnitOnTile = null;
		foreach (var u in tile.unitsOnTile)
			if (u.guid == looseView.mapView.game.CurrentlySelectedUnit.guid) {
				looseView.DrawCircle(tileCenter, 16, Color.Color8(255, 255, 0));
				selectedUnitOnTile = u;
			}
		var unit = (selectedUnitOnTile != null) ? selectedUnitOnTile : tile.findTopDefender();

		// Draw colored circle at unit's feet to show who owns it
		looseView.DrawCircle(tileCenter, 8, new Color(unit.owner.color));

		int iconIndex = unit.unitType.iconIndex;
		Vector2 iconUpperLeft = new Vector2(1 + 33 * (iconIndex % unitIconsWidth), 1 + 33 * (iconIndex / unitIconsWidth));
		Rect2 unitRect = new Rect2(iconUpperLeft, new Vector2(32, 32));
		Rect2 screenRect = new Rect2(tileCenter - new Vector2(24, 40), new Vector2(48, 48));
		looseView.DrawTextureRectRegion(unitIcons, screenRect, unitRect);

		Vector2 indicatorLoc = tileCenter - new Vector2(26, 40);

		int mp = unit.movementPointsRemaining;
		int moveIndIndex = (mp <= 0) ? 4 : ((mp >= unit.unitType.movement) ? 0 : 2);
		Vector2 moveIndUpperLeft = new Vector2(1 + 7 * moveIndIndex, 1);
		Rect2 moveIndRect = new Rect2(moveIndUpperLeft, new Vector2(6, 6));
		screenRect = new Rect2(indicatorLoc, new Vector2(6, 6));
		looseView.DrawTextureRectRegion(unitMovementIndicators, screenRect, moveIndRect);

		int hpIndHeight = 20, hpIndWidth = 6;
		var hpIndBackgroundRect = new Rect2(indicatorLoc + new Vector2(-1, 8), new Vector2(hpIndWidth, hpIndHeight));
		if ((unit.unitType.attack > 0) || (unit.unitType.defense > 0)) {
			float hpFraction = (float)unit.hitPointsRemaining / unit.maxHitPoints;
			looseView.DrawRect(hpIndBackgroundRect, Color.Color8(0, 0, 0));
			float hpHeight = hpFraction * (hpIndHeight - 2);
			if (hpHeight < 1)
				hpHeight = 1;
			var hpContentsRect = new Rect2(hpIndBackgroundRect.Position + new Vector2(1, hpIndHeight - 1 - hpHeight), // position
										   new Vector2(hpIndWidth - 2, hpHeight)); // size
			looseView.DrawRect(hpContentsRect, getHPColor(hpFraction));
			if (unit.isFortified)
				looseView.DrawRect(hpIndBackgroundRect, white, false);
		}

		// Draw lines to show that there are more units on this tile
		if (tile.unitsOnTile.Count > 1) {
			int lineCount = tile.unitsOnTile.Count;
			if (lineCount > 5)
				lineCount = 5;
			for (int n = 0; n < lineCount; n++) {
				var lineStart = indicatorLoc + new Vector2(-2, hpIndHeight + 12 + 4 * n);
				looseView.DrawLine(lineStart                    , lineStart + new Vector2(8, 0), white);
				looseView.DrawLine(lineStart + new Vector2(0, 1), lineStart + new Vector2(8, 1), Color.Color8(75, 75, 75));
			}
		}
	}
}

public class BuildingLayer : LooseLayer {
	private ImageTexture buildingsTex;
	private Vector2 buildingSpriteSize;

	public BuildingLayer()
	{
		var buildingsPCX = new Pcx(Util.Civ3MediaPath("Art/Terrain/TerrainBuildings.PCX"));
		buildingsTex = PCXToGodot.getImageTextureFromPCX(buildingsPCX);
		buildingSpriteSize = new Vector2((float)buildingsTex.GetWidth() / 3, (float)buildingsTex.GetHeight() / 4);
	}

	public override void drawObject(LooseView looseView, Tile tile, Vector2 tileCenter)
	{
		if (tile.hasBarbarianCamp) {
			var texRect = new Rect2(buildingSpriteSize * new Vector2 (2, 0), buildingSpriteSize);	//(2, 0) is the offset in the TerrainBuildings.PCX file (top row, third in)
			// TODO: Modify this calculation so it doesn't assume buildingSpriteSize is the same as the size of the terrain tiles
			var screenRect = new Rect2(tileCenter - (float)0.5 * buildingSpriteSize, buildingSpriteSize);
			looseView.DrawTextureRectRegion(buildingsTex, screenRect, texRect);
		}
	}
}

public class CityLayer : LooseLayer {
	private ImageTexture cityTexture;
	private Vector2 citySpriteSize;

	public CityLayer()
	{
		//TODO: Generalize, support multiple city types, etc.
		this.cityTexture = Util.LoadTextureFromPCX("Art/Cities/rROMAN.PCX", 0, 0, 167, 95);
		this.citySpriteSize = new Vector2(167, 95);
	}

	public override void drawObject(LooseView looseView, Tile tile, Vector2 tileCenter)
	{
		if (tile.cityAtTile != null) {
			City city = tile.cityAtTile;
			GD.Print("Tile " + tile.xCoordinate + ", " + tile.yCoordinate + " has a city named " + city.name);
			Rect2 screenRect = new Rect2(tileCenter - (float)0.5 * citySpriteSize, citySpriteSize);
			Rect2 textRect = new Rect2(new Vector2(0, 0), citySpriteSize);
			looseView.DrawTextureRectRegion(cityTexture, screenRect, textRect);

			DynamicFont smallFont = new DynamicFont();
			smallFont.FontData = ResourceLoader.Load("res://Fonts/NotoSans-Regular.ttf") as DynamicFontData;
			smallFont.Size = 11;

			String cityNameAndGrowth = city.name + " : 10";
			String productionDescription = "Warrior : 5";

			int cityNameAndGrowthWidth = (int)smallFont.GetStringSize(cityNameAndGrowth).x;
			int productionDescriptionWidth = (int)smallFont.GetStringSize(productionDescription).x;
			int maxTextWidth = Math.Max(cityNameAndGrowthWidth, productionDescriptionWidth);
			GD.Print("Width of city name = " + maxTextWidth);

			int cityLabelWidth = maxTextWidth + (city.IsCapital()? 70 : 45);	//TODO: Is 65 right?  70?  Will depend on whether it's capital, too
			int textAreaWidth = cityLabelWidth - (city.IsCapital() ? 50 : 25);
			GD.Print("City label width: " + cityLabelWidth);
			GD.Print("Text area width: " + textAreaWidth);
			const int CITY_LABEL_HEIGHT = 23;
			const int TEXT_ROW_HEIGHT = 9;
			const int LEFT_RIGHT_BOXES_WIDTH = 24;
			const int LEFT_RIGHT_BOXES_HEIGHT = CITY_LABEL_HEIGHT - 2;

			//Label/name/producing area
			Image labelImage = new Image();
			labelImage.Create(cityLabelWidth, CITY_LABEL_HEIGHT, false, Image.Format.Rgba8);
			labelImage.Fill(Color.Color8(0, 0, 0, 0));
			byte transparencyLevel = 192;	//25%
			Color civColor = Color.Color8(227, 10, 10, transparencyLevel);	//Roman Red
			Color civColorDarker = Color.Color8(0, 0, 138, transparencyLevel);	//todo: automate the darker() function.  maybe less transparency?
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
			
				Pcx cityIcons = Util.LoadPCX("Art/Cities/city icons.pcx");
				Image nonEmbassyStar = PCXToGodot.getImageFromPCX(cityIcons, 20, 1, 18, 18);
				labelImage.BlendRect(nonEmbassyStar, new Rect2(0, 0, new Vector2(18, 18)), new Vector2(cityLabelWidth - 24, 2));
			}

			//todo: darker shades of civ color around edges

			ImageTexture label = new ImageTexture();
			label.CreateFromImage(labelImage, 0);

			Rect2 labelDestination = new Rect2(tileCenter + new Vector2(cityLabelWidth/-2, 24), new Vector2(cityLabelWidth, CITY_LABEL_HEIGHT));	//24 is a swag
			Rect2 allOfTheLabel = new Rect2(new Vector2(0, 0), new Vector2(cityLabelWidth, CITY_LABEL_HEIGHT));
			looseView.DrawTextureRectRegion(label, labelDestination, allOfTheLabel);

			//Destination for font is based on lower-left of baseline of font, not upper left as for blitted rectangles
			int cityNameOffset = cityNameAndGrowthWidth/-2;
			int prodDescriptionOffset = productionDescriptionWidth/-2;
			if (!city.IsCapital()) {
				cityNameOffset+=12;
				prodDescriptionOffset+=12;
			}
			Vector2 cityNameDestination = new Vector2(tileCenter + new Vector2(cityNameOffset, 24) + new Vector2(0, 10));
			looseView.DrawString(smallFont, cityNameDestination, cityNameAndGrowth, Color.Color8(255, 255, 255, 255));
			Vector2 productionDestination = new Vector2(tileCenter + new Vector2(prodDescriptionOffset, 24) + new Vector2(0, 20));
			looseView.DrawString(smallFont, productionDestination, productionDescription, Color.Color8(255, 255, 255, 255));

			//City pop size
			DynamicFont midSizedFont = new DynamicFont();
			midSizedFont.FontData = ResourceLoader.Load("res://Fonts/NotoSans-Regular.ttf") as DynamicFontData;
			midSizedFont.Size = 18;
			string popSizeString = "24";
			int popSizeWidth = (int)midSizedFont.GetStringSize(popSizeString).x;
			int popSizeOffset = LEFT_RIGHT_BOXES_WIDTH/2 - popSizeWidth/2;
			Vector2 popSizeDestination = new Vector2(tileCenter + new Vector2(cityLabelWidth/-2, 24) + new Vector2(popSizeOffset, 18));
			looseView.DrawString(midSizedFont, popSizeDestination, popSizeString, Color.Color8(255, 255, 255, 255));
		}
	}
}

public class LooseView : Node2D {
	public MapView mapView;
	public List<LooseLayer> layers = new List<LooseLayer>();

	public LooseView(MapView mapView)
	{
		this.mapView = mapView;
	}

	public override void _Draw()
	{
		base._Draw();

		var map = MapInteractions.GetWholeMap();
		foreach (var layer in layers.FindAll(L => L.visible))
			foreach (var vT in mapView.visibleTiles()) {
				int x = mapView.wrapTileX(vT.virtTileX);
				int y = mapView.wrapTileY(vT.virtTileY);
				var tile = map.tileAt(x, y);
				Vector2 tileCenter = MapView.cellSize * new Vector2(x + 1, y + 1);
				layer.drawObject(this, tile, tileCenter);
			}
	}
}

public class MapView : Node2D {
	// cellSize is half the size of the tile sprites, or the amount of space each tile takes up when they are packed on the grid (note tiles are
	// staggered and half overlap).
	public static readonly Vector2 cellSize = new Vector2(64, 32);
	public Vector2 scaledCellSize {
		get { return cellSize * new Vector2(cameraZoom, cameraZoom); }
	}

	public Game game;

	public int mapWidth  { get; private set; }
	public int mapHeight { get; private set; }
	public bool wrapHorizontally { get; private set; }
	public bool wrapVertically   { get; private set; }

	private Vector2 internalCameraLocation = new Vector2(0, 0);
	public Vector2 cameraLocation {
		get {
			return internalCameraLocation;
		}
		set {
			setCameraLocation(value);
		}
	}
	public float internalCameraZoom = 1;
	public float cameraZoom {
		get { return internalCameraZoom; }
		set { setCameraZoomFromMiddle(value); }
	}

	public struct VisibleTile {
		public int virtTileX, virtTileY; // (x, y) coords of the tile. These are "virtual", i.e. unwrapped, coordinates.
	}

	private LooseView looseView;
	private ShaderMaterial testShaderMaterial;

	public GridLayer gridLayer { get; private set; }

	public MapView(Game game, int mapWidth, int mapHeight, bool wrapHorizontally, bool wrapVertically)
	{
		this.game = game;
		this.mapWidth = mapWidth;
		this.mapHeight = mapHeight;
		this.wrapHorizontally = wrapHorizontally;
		this.wrapVertically = wrapVertically;

		looseView = new LooseView(this);
		looseView.layers.Add(new TerrainLayer());
		gridLayer = new GridLayer();
		looseView.layers.Add(gridLayer);
		looseView.layers.Add(new BuildingLayer());
		looseView.layers.Add(new UnitLayer());
		looseView.layers.Add(new CityLayer());

		AddChild(looseView);

		// var (units32Palette, units32Indices) = loadPalettizedPCX("Art/Units/units_32.pcx");
		// testShaderMaterial = createTestShaderMaterial((units32Palette, units32Indices), new Vector2(32, 32));
		// AddChild(createInstancedMeshTest(testShaderMaterial, units32Indices));
		var flicTest = createFlicTest();
		testShaderMaterial = flicTest.Material as ShaderMaterial;
		flicTest.Position = new Vector2(300, 300);
		flicTest.Scale = new Vector2(1, -1);
		AddChild(flicTest);

		onVisibleAreaChanged();
	}

	public override void _Process(float delta)
	{
		var ts = (double)OS.GetTicksMsec();

		var r = 0.5 + 0.5 * Math.Sin(ts / 200.0);
		var g = 0.5 + 0.5 * Math.Cos(ts / 200.0);
		var b = 0.5 + 0.5 * Math.Sin(ts / 400.0);
		testShaderMaterial.SetShaderParam("civColor", new Vector3((float)r, (float)g, (float)b));

		testShaderMaterial.SetShaderParam("spriteXY", new Vector2((int)(ts / 100.0) % 10, 0));
	}

	// Creates a texture from raw palette data. The data must be 256 pixels by 3 channels. Returns a 16x16 unfiltered RGB texture.
	// TODO: Move this to Util with the other PCX loading stuff
	public static ImageTexture createPaletteTexture(byte[,] raw)
	{
		if ((raw.GetLength(0) != 256) || (raw.GetLength(1) != 3))
			throw new Exception("Invalid palette dimensions. Palettes must be 256x3.");

		// Flatten palette data since CreateFromData can't accept two-dimensional arrays
		byte[] flatPalette = new byte[3*256];
		for (int n = 0; n < 256; n++)
			for (int k = 0; k < 3; k++)
				flatPalette[k + 3 * n] = raw[n, k];

		var img = new Image();
		img.CreateFromData(16, 16, false, Image.Format.Rgb8, flatPalette);
		var tex = new ImageTexture();
		tex.CreateFromImage(img, 0);
		return tex;
	}

	// Creates textures from a PCX file without de-palettizing it. Returns two ImageTextures, the first is 16x16 with RGB8 format containing the
	// color palette and the second is the size of the image itself and contains the indices in R8 format.
	// TODO: Move this to Util with the other PCX loading stuff
	public static (ImageTexture palette, ImageTexture indices) loadPalettizedPCX(string file_path)
	{
		var pcx = Util.LoadPCX(file_path);

		var imgIndices = new Image();
		imgIndices.CreateFromData(pcx.Width, pcx.Height, false, Image.Format.R8, pcx.ColorIndices);
		var texIndices = new ImageTexture();
		texIndices.CreateFromImage(imgIndices, 0);

		return (createPaletteTexture(pcx.Palette), texIndices);
	}

	public ShaderMaterial createTestShaderMaterial((ImageTexture, ImageTexture) paletteAndIndices, Vector2 spriteSize)
	{
		// It would make more sense to use a usampler2D for the indices but that doesn't work. As far as I can tell, (u)int samplers are
		// broken on Godot because there's no way to create a texture with a compatible format. See:
		// https://www.khronos.org/opengl/wiki/Sampler_(GLSL)#Sampler_types - Says it's undefined behavior to read from a usampler2d if the
		// attached texture format is not GL_R8UI.
		// https://docs.godotengine.org/en/stable/classes/class_image.html#enum-image-format - None of the Godot texture formats correspond to
		// GL_R8UI. The closest is FORMAT_R8 which corresponds to GL_RED except that won't work since it's a floating point format.
		string shaderSource = @"
		shader_type canvas_item;
		uniform sampler2D palette;
		uniform sampler2D civColorWhitePalette;
		uniform sampler2D indices;
		uniform vec2 relSpriteSize; // sprite size relative to the entire sheet
		uniform vec2 spriteXY; // coordinates of the sprite to be drawn, in number of sprites not pixels
		uniform vec3 civColor;

		vec4 sampleCivTintedColor(vec2 paletteCoords)
		{
			return vec4(civColor, 1.0) * texture(civColorWhitePalette, paletteCoords);
		}

		void vertex()
		{
			UV = (spriteXY + UV) * relSpriteSize;
		}

		void fragment()
		{
			int colorIndex = int(255.0 * texture(indices, UV).r);
			if (colorIndex >= 254) // indices 254 and 255 are transparent
				discard;
			vec2 paletteCoords = vec2(float(colorIndex % 16), float(colorIndex / 16)) / 16.0;
			bool tintedByCiv = (colorIndex < 16) || ((colorIndex < 64) && (colorIndex % 2 == 0));
			if (tintedByCiv)
				COLOR = sampleCivTintedColor(paletteCoords);
			else
				COLOR = texture(palette, paletteCoords);
		}
		";
		var shader = new Shader();
		shader.Code = shaderSource;

		var (civColorWhitePalette, _) = loadPalettizedPCX("Art/Units/Palettes/ntp00.pcx");
		var (palette, indices) = paletteAndIndices;

		var tr = new ShaderMaterial();
		tr.Shader = shader;
		tr.SetShaderParam("palette", palette);
		tr.SetShaderParam("civColorWhitePalette", civColorWhitePalette);
		tr.SetShaderParam("indices", indices);
		var indicesDims = new Vector2(indices.GetWidth(), indices.GetHeight());
		tr.SetShaderParam("relSpriteSize", spriteSize / indicesDims);
		tr.SetShaderParam("spriteXY", new Vector2(0, 0));
		tr.SetShaderParam("civColor", new Vector3(0.4f, 0.4f, 1));
		return tr;
	}


	public MeshInstance2D createFlicTest()
	{
		var flic = new Flic(Util.Civ3MediaPath("Art/Units/warrior/warriorRun.flc"));

		var palette = createPaletteTexture(flic.Palette);

		var countColumns = flic.Images.GetLength(1); // Each column contains one frame
		var countRows = flic.Images.GetLength(0); // Each row contains one animation
		var countImages = countColumns * countRows;

		byte[] allIndices = new byte[countRows * countColumns * flic.Width * flic.Height];
		// row, col loop over the sprites, each one a frame of the animation
		for (int row = 0; row < countRows; row++)
			for (int col = 0; col < countColumns; col++)
				// x, y loop over pixels within each sprite
				for (int y = 0; y < flic.Height; y++)
					for (int x = 0; x < flic.Width; x++) {
						int pixelRow = row * flic.Height + y,
						    pixelCol = col * flic.Width + x,
						    pixelIndex = pixelRow * countColumns * flic.Width + pixelCol;
						allIndices[pixelIndex] = flic.Images[row, col][y * flic.Width + x];
					}

		var imgIndices = new Image();
		imgIndices.CreateFromData(countColumns * flic.Width, countRows * flic.Height, false, Image.Format.R8, allIndices);
		var texIndices = new ImageTexture();
		texIndices.CreateFromImage(imgIndices, 0);

		var quad = new QuadMesh();
		quad.Size = new Vector2(600, 600);

		var tr = new MeshInstance2D();
		tr.Material = createTestShaderMaterial((palette, texIndices), new Vector2(flic.Width, flic.Height));
		tr.Mesh = quad;
		return tr;
	}

	public MultiMeshInstance2D createInstancedMeshTest(ShaderMaterial shaderMaterial, ImageTexture indices)
	{
		var indicesDims = new Vector2(indices.GetWidth(), indices.GetHeight());

		var quad = new QuadMesh();
		quad.Size = new Vector2(32, 32); // Same as sprite size

		var mm = new MultiMesh();
		mm.TransformFormat = MultiMesh.TransformFormatEnum.Transform2d;
		mm.ColorFormat = MultiMesh.ColorFormatEnum.None;
		mm.CustomDataFormat = MultiMesh.CustomDataFormatEnum.Float;
		mm.Mesh = quad;

		mm.InstanceCount = 70;
		for (int n = 0; n < mm.InstanceCount; n++) {
			var tform = new Transform2D(0, new Vector2(50 + 12 * n, 150 + 100 * (float)Math.Sin(0.75f * n)));
			tform.Scale = new Vector2(1, -1); // Flip vertically
			mm.SetInstanceTransform2d(n, tform);
			int spriteIndex = n;
			var spriteOffset = new Vector2(1 + 33 * (spriteIndex%14), 1 + 33 * (spriteIndex/14)) / indicesDims;
			mm.SetInstanceCustomData(n, new Color(spriteOffset.x, spriteOffset.y, 0, 0));
		}

		var tr = new MultiMeshInstance2D();
		tr.Material = shaderMaterial;
		tr.Multimesh = mm;
		return tr;
	}

	public bool isRowAt(int y)
	{
		return wrapVertically || ((y >= 0) && (y < mapHeight));
	}

	// TODO: Use function from GameMap (in C7GameData). Also copy isRowAt over there.
	public bool isTileAt(int x, int y)
	{
		bool evenRow = y%2 == 0;
		bool xInBounds; {
			if (wrapHorizontally)
				xInBounds = true;
			else if (evenRow)
				xInBounds = (x >= 0) && (x <= mapWidth - 2);
			else
				xInBounds = (x >= 1) && (x <= mapWidth - 1);
		}
		return isRowAt(y) && xInBounds && (evenRow ? (x%2 == 0) : (x%2 != 0));
	}

	public int wrapTileX(int x)
	{
		if (wrapHorizontally) {
			int tr = x % mapWidth;
			return (tr >= 0) ? tr : tr + mapWidth;
		} else
			return x;
	}

	public int wrapTileY(int y)
	{
		if (wrapVertically) {
			int tr = y % mapHeight;
			return (tr >= 0) ? tr : tr + mapHeight;
		} else
			return y;
	}

	// Returns the size in pixels of the area in which the map will be drawn. This is the viewport size or, if that's null, the window size.
	public Vector2 getVisibleAreaSize()
	{
		var viewport = GetViewport();
		return (viewport != null) ? viewport.Size : OS.WindowSize;
	}

	public IEnumerable<VisibleTile> visibleTiles()
	{
		int upperLeftX, upperLeftY;
		tileCoordsOnScreenAt(new Vector2(0, 0), out upperLeftX, out upperLeftY);
		Vector2 mapViewSize = new Vector2(2, 4) + getVisibleAreaSize() / scaledCellSize;
		for (int dy = -2; dy < mapViewSize.y; dy++) {
			int y = upperLeftY + dy;
			if (isRowAt(y))
				for (int dx = -2 + dy%2; dx < mapViewSize.x; dx += 2) {
					int x = upperLeftX + dx;
					if (isTileAt(x, y)) {
						VisibleTile tileInView = new VisibleTile();
						tileInView.virtTileX = x;
						tileInView.virtTileY = y;
						yield return tileInView;
					}
				}
		}
	}

	public void onVisibleAreaChanged()
	{
		// TODO: Update this comment and move it somewhere more appropriate
		// MapView is not the entire game map, rather it is a window into the game map that stays near the origin and covers the entire
		// screen. For small movements, the MapView itself is moved (amount is in cameraResidueX/Y) but once the movement equals an entire
		// grid cell (2 times the tile width or height) the map is snapped back toward the origin by that amount and to compensate it changes
		// what tiles are drawn (cameraTileX/Y). The advantage to doing things this way is that it makes it easy to duplicate tiles around
		// wrapped edges.

		looseView.Position = -cameraLocation;
		looseView.Update(); // trigger redraw
	}

	// "center" is the screen location around which the zoom is centered, e.g., if center is (0, 0) the tile in the top left corner will be the
	// same after the zoom level is changed, and if center is screenSize/2, the tile in the center of the window won't change.
	// This function does not adjust the zoom slider, so to keep the slider in sync with the actual zoom level, use AdjustZoomSlider. This
	// function must be separate, though, so that we can change the zoom level inside that callback without entering an infinite loop.
	public void setCameraZoom(float newScale, Vector2 center)
	{
		var v2NewZoom = new Vector2(newScale, newScale);
		var v2OldZoom = new Vector2(cameraZoom, cameraZoom);
		if (v2NewZoom != v2OldZoom) {
			internalCameraZoom = newScale;
			looseView.Scale = v2NewZoom;
			setCameraLocation ((v2NewZoom / v2OldZoom) * (cameraLocation + center) - center);
			// resetVisibleTiles(); // Don't have to call this because it's already called when the camera location is changed
		}
	}

	// Zooms in or out centered on the middle of the screen
	public void setCameraZoomFromMiddle(float newScale)
	{
		setCameraZoom(newScale, getVisibleAreaSize() / 2);
	}

	public void moveCamera(Vector2 offset)
	{
		setCameraLocation(cameraLocation + offset);
	}

	public void setCameraLocation(Vector2 location)
	{
		// Prevent the camera from moving beyond an unwrapped edge of the map. One complication here is that the viewport might actually be
		// larger than the map (if we're zoomed far out) so in that case we must apply the constraint the other way around, i.e. constrain the
		// map to the viewport rather than the viewport to the map.
		// TODO: Not quite perfect. When you zoom out you can still move the map a bit off the right/bottom edges.
		Vector2 visAreaSize = getVisibleAreaSize();
		Vector2 mapPixelSize = new Vector2(cameraZoom, cameraZoom) * (new Vector2(cellSize.x * (mapWidth + 1), cellSize.y * (mapHeight + 1)));
		if (!wrapHorizontally) {
			float leftLim, rightLim;
			{
				if (mapPixelSize.x >= visAreaSize.x) {
					leftLim = 0;
					rightLim = mapPixelSize.x - visAreaSize.x;
				} else {
					leftLim = mapPixelSize.x - visAreaSize.x;
					rightLim = 0;
				}
			}
			if (location.x < leftLim)
				location.x = leftLim;
			else if (location.x > rightLim)
				location.x = rightLim;
		}
		if (!wrapVertically) {
			// These margins allow the player to move the camera that far off those map edges so that the UI controls don't cover up the
			// map. TODO: These values should be read from the sizes of the UI elements instead of hardcoded.
			float topMargin = 70, bottomMargin = 140;
			float topLim, bottomLim;
			{
				if (mapPixelSize.y >= visAreaSize.y) {
					topLim = -topMargin;
					bottomLim = mapPixelSize.y - visAreaSize.y + bottomMargin;
				} else {
					topLim = mapPixelSize.y - visAreaSize.y;
					bottomLim = 0;
				}
			}
			if (location.y < topLim)
				location.y = topLim;
			else if (location.y > bottomLim)
				location.y = bottomLim;
		}

		internalCameraLocation = location;
		onVisibleAreaChanged();
	}

	public Vector2 screenLocationOfTileCoords(int x, int y, bool center = true)
	{
		// Add one to x & y to get the tile center b/c in Civ 3 the tile at (x, y) is a diamond centered on (x+1, y+1).
		Vector2 centeringOffset = center ? new Vector2(1, 1) : new Vector2(0, 0);

		var mapLoc = (new Vector2(x, y) + centeringOffset) * cellSize;
		return mapLoc * cameraZoom - cameraLocation;
	}

	// Returns the location of tile (x, y) on the screen, if "center" is true returns the location of the tile center and otherwise returns the
	// upper left. Works even if (x, y) is off screen or out of bounds.
	public Vector2 screenLocationOfTile(Tile tile, bool center = true)
	{
		return screenLocationOfTileCoords(tile.xCoordinate, tile.yCoordinate, center);
	}

	public void tileCoordsOnScreenAt(Vector2 screenLocation, out int x, out int y)
	{
		Vector2 mapLoc = (screenLocation + cameraLocation) / scaledCellSize;
		Vector2 intMapLoc = mapLoc.Floor();
		Vector2 fracMapLoc = mapLoc - intMapLoc;
		x = (int)intMapLoc.x;
		y = (int)intMapLoc.y;
		bool evenColumn = x%2 == 0, evenRow = y%2 == 0;
		if (evenColumn ^ evenRow) {
			if (fracMapLoc.y > fracMapLoc.x)
				x -= 1;
			else
				y -= 1;
		} else {
			if (fracMapLoc.y < 1 - fracMapLoc.x) {
				x -= 1;
				y -= 1;
			}
		}
	}

	// Returns the coordinates of the tile at the given screen location and true if there is one, otherwise returns (-1, -1) and false.
	public Tile tileOnScreenAt(Vector2 screenLocation)
	{
		int x, y;
		tileCoordsOnScreenAt(screenLocation, out x, out y);
		return MapInteractions.GetWholeMap().tileAt(wrapTileX(x), wrapTileY(y));
	}

	public void centerCameraOnTile(Tile t)
	{
		var tileCenter = new Vector2(t.xCoordinate + 1, t.yCoordinate + 1) * scaledCellSize;
		setCameraLocation(tileCenter - (float)0.5 * getVisibleAreaSize());
	}
}
