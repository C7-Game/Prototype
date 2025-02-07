using Godot;
using System;
using Serilog;
using System.Collections.Generic;
using C7GameData;


// Handles the city screen, where citizens can be assigned and other details of
// the city can bee seen.
public partial class CityScreen : CenterContainer {
	private ILogger log = LogManager.ForContext<CityScreen>();

	// Called when the node enters the scene tree for the first time.
	public override void _Ready() {
		TextureRect background = new() {
			Texture = Util.LoadTextureFromPCX("Art/city screen/background.pcx")
		};
		AddChild(background);

		TextureButton close = new() {
			TextureNormal = Util.LoadTextureFromPCX("Art/city screen/cityMgmtButtons.pcx", 155, 1, 32, 48),
			TextureHover = Util.LoadTextureFromPCX("Art/city screen/cityMgmtButtons.pcx", 155, 50, 32, 48),
			TexturePressed = Util.LoadTextureFromPCX("Art/city screen/cityMgmtButtons.pcx", 155, 99, 32, 48)
		};
		close.SetPosition(new Vector2(950, 20));
		close.Pressed += () => { this.Hide(); };
		background.AddChild(close);

		this.Hide();
	}

	private void OnShowCityScreen(ParameterWrapper<City> city) {
		this.Show();
	}
}
