using System.Collections.Generic;
using Godot;
using C7GameData;
using Serilog;
using C7Engine;

[GlobalClass]
[Tool]
public partial class MiniMap : Control {
	private ILogger log = LogManager.ForContext<MiniMap>();

	private MapView mapView;

	private MiniMapFrame frame;
	private List<MiniMapLayer> layers;
	private MiniMapControls controls;

	public MiniMap(MapView mapView) {
		this.mapView = mapView;
	}

	public override void _Ready() {
		frame = new MiniMapFrame(mapView);
		AddChild(frame);

		layers = new List<MiniMapLayer>
		{
			new BaseLandMiniLayer(),
			new TerrainMiniLayer(),
			new PlayerColorMiniLayer(),
			new CityMiniLayer(),
			new WaterMiniLayer(),
			new FogOfWarMiniLayer()
		};

		controls = new MiniMapControls();
		AddChild(controls);

		MouseFilter = MouseFilterEnum.Pass;
	}

	// TODO: Enable/disable via INI
	// TODO: Dynamic colours
	// TODO: Configurable colours
	// TODO: Resizing minimap
	// TODO: Configurable size via INI (absolute/relative)

	public override void _Process(double delta) {
		frame.SetViewportPosition();

		EngineStorage.ReadGameData((GameData gD) => {
			var map = gD.map;

			var mapImage = Image.CreateEmpty(map.numTilesWide, map.numTilesTall / 2, true, Image.Format.Rgba8);

			// Configure layers
			foreach (var layer in layers)
				layer.Configure(gD);

			// Draw tiles as pixels, layer at a time
			foreach (var t in map.tiles) {
				var (x, y) = ComputeIsoCoordinates(t);
				foreach (var layer in layers)
					layer.DrawTile(mapImage, t, x, y);
			}

			// Draw the viewport bounds as a rectangle
			if (mapView != null) {
				var vr = mapView.getVisibleRegion();
				controls.DrawBounds(mapImage, map, vr);
			}

			// Render the image
			frame.RenderImage(mapImage);
		});
	}

	private (int x, int y) ComputeIsoCoordinates(Tile tile) {
		// Isometric tile dimensions - wider than tall for rhombus shape
		var x = tile.XCoordinate;
		var y = tile.YCoordinate / 2;
		return (x, y);
	}
}
