using Godot;
using C7GameData;

public partial class TestCityUI : Node2D
{
	private CityUI cityUI;
	private CanvasLayer canvas = new CanvasLayer();
	public override void _Ready() {
		PackedScene scene = GD.Load<PackedScene>("res://CityUI.tscn");
		cityUI = scene.Instantiate<CityUI>();
		AddChild(canvas);
		canvas.AddChild(cityUI);

		City city = new City(Tile.NONE, null, "Hogsmeade");
		cityUI.SetCity(city);
	}
}
