using Godot;
using System;
using C7Engine;
using C7Engine.Lua;
using C7GameData.Save;
using Serilog;

[Tool]
public partial class WorldSetup : Control {
	private static ILogger log = LogManager.ForContext<WorldSetup>();

	[Export] TextureRect background;

	[Export] Label pangaeaLabel;
	[Export] Label continentsLabel;
	[Export] Label archipelagoLabel;

	[Export] TextureButton pangaea60;
	[Export] TextureButton pangaea70;
	[Export] TextureButton pangaea80;

	[Export] TextureButton continents60;
	[Export] TextureButton continents70;
	[Export] TextureButton continents80;

	[Export] TextureButton archipelago60;
	[Export] TextureButton archipelago70;
	[Export] TextureButton archipelago80;

	[Export] TextureRect pangaea60Large;
	[Export] TextureRect pangaea70Large;
	[Export] TextureRect pangaea80Large;
	[Export] TextureRect continents60Large;
	[Export] TextureRect continents70Large;
	[Export] TextureRect continents80Large;
	[Export] TextureRect archipelago60Large;
	[Export] TextureRect archipelago70Large;
	[Export] TextureRect archipelago80Large;

	[Export] TextureButton arid;
	[Export] TextureButton normal;
	[Export] TextureButton wet;
	[Export] TextureButton cool;
	[Export] TextureButton temperate;
	[Export] TextureButton warm;
	[Export] TextureButton billion3;
	[Export] TextureButton billion4;
	[Export] TextureButton billion5;

	[Export] TextureRect aridLarge;
	[Export] TextureRect normalLarge;
	[Export] TextureRect wetLarge;
	[Export] TextureRect coolLarge;
	[Export] TextureRect temperateLarge;
	[Export] TextureRect warmLarge;
	[Export] TextureRect billion3Large;
	[Export] TextureRect billion4Large;
	[Export] TextureRect billion5Large;

	[Export] CheckButton tinySize;
	[Export] CheckButton smallSize;
	[Export] CheckButton standardSize;
	[Export] CheckButton largeSize;
	[Export] CheckButton hugeSize;
	[Export] CheckButton randomSize;

	[Export] TextureButton confirm;
	[Export] TextureButton cancel;

	[Export] LineEdit seedInput;

	WorldCharacteristics.Landform landform = WorldCharacteristics.Landform.Pangaea;
	WorldCharacteristics.OceanCoverage ocean = WorldCharacteristics.OceanCoverage.Percent_70;
	WorldCharacteristics.Age age = WorldCharacteristics.Age.Billion_4;
	WorldCharacteristics.Temperature temp = WorldCharacteristics.Temperature.Temperate;
	WorldCharacteristics.Climate clim = WorldCharacteristics.Climate.Normal;

	// Called when the node enters the scene tree for the first time.
	public override void _Ready() {
		background.Texture = TextureLoader.Load("world_setup.background");

		TextureLoader.SetButtonTextures(pangaea80, "world_setup.pangaea80");
		pangaea80.Pressed += () => {
			landform = WorldCharacteristics.Landform.Pangaea;
			ocean = WorldCharacteristics.OceanCoverage.Percent_80;
			ResetLandformGraphics();
			pangaea80Large.Visible = true;
			pangaeaLabel.Text = "Pangaea (80% water)";
		};

		TextureLoader.SetButtonTextures(pangaea70, "world_setup.pangaea70");
		pangaea70.Pressed += () => {
			landform = WorldCharacteristics.Landform.Pangaea;
			ocean = WorldCharacteristics.OceanCoverage.Percent_70;
			ResetLandformGraphics();
			pangaea70Large.Visible = true;
			pangaeaLabel.Text = "Pangaea (70% water)";
		};

		TextureLoader.SetButtonTextures(pangaea60, "world_setup.pangaea60");
		pangaea60.Pressed += () => {
			landform = WorldCharacteristics.Landform.Pangaea;
			ocean = WorldCharacteristics.OceanCoverage.Percent_60;
			ResetLandformGraphics();
			pangaea60Large.Visible = true;
			pangaeaLabel.Text = "Pangaea (60% water)";
		};

		TextureLoader.SetButtonTextures(continents80, "world_setup.continents80");
		continents80.Pressed += () => {
			landform = WorldCharacteristics.Landform.Continents;
			ocean = WorldCharacteristics.OceanCoverage.Percent_80;
			ResetLandformGraphics();
			continents80Large.Visible = true;
			continentsLabel.Text = "Continents (80% water)";
		};

		TextureLoader.SetButtonTextures(continents70, "world_setup.continents70");
		continents70.Pressed += () => {
			landform = WorldCharacteristics.Landform.Continents;
			ocean = WorldCharacteristics.OceanCoverage.Percent_70;
			ResetLandformGraphics();
			continents70Large.Visible = true;
			continentsLabel.Text = "Continents (70% water)";
		};

		TextureLoader.SetButtonTextures(continents60, "world_setup.continents60");
		continents60.Pressed += () => {
			landform = WorldCharacteristics.Landform.Continents;
			ocean = WorldCharacteristics.OceanCoverage.Percent_60;
			ResetLandformGraphics();
			continents60Large.Visible = true;
			continentsLabel.Text = "Continents (60% water)";
		};

		TextureLoader.SetButtonTextures(archipelago80, "world_setup.archipelago80");
		archipelago80.Pressed += () => {
			landform = WorldCharacteristics.Landform.Archipelago;
			ocean = WorldCharacteristics.OceanCoverage.Percent_80;
			ResetLandformGraphics();
			archipelago80Large.Visible = true;
			archipelagoLabel.Text = "Archipelago (80% water)";
		};

		TextureLoader.SetButtonTextures(archipelago70, "world_setup.archipelago70");
		archipelago70.Pressed += () => {
			landform = WorldCharacteristics.Landform.Archipelago;
			ocean = WorldCharacteristics.OceanCoverage.Percent_70;
			ResetLandformGraphics();
			archipelago70Large.Visible = true;
			archipelagoLabel.Text = "Archipelago (70% water)";
		};

		TextureLoader.SetButtonTextures(archipelago60, "world_setup.archipelago60");
		archipelago60.Pressed += () => {
			landform = WorldCharacteristics.Landform.Archipelago;
			ocean = WorldCharacteristics.OceanCoverage.Percent_60;
			ResetLandformGraphics();
			archipelago60Large.Visible = true;
			archipelagoLabel.Text = "Archipelago (60% water)";
		};

		TextureLoader.SetButtonTextures(arid, "world_setup.arid");
		arid.Pressed += () => {
			clim = WorldCharacteristics.Climate.Arid;
			ResetClimateGraphics();
			aridLarge.Visible = true;
		};

		TextureLoader.SetButtonTextures(normal, "world_setup.normal");
		normal.Pressed += () => {
			clim = WorldCharacteristics.Climate.Normal;
			ResetClimateGraphics();
			normalLarge.Visible = true;
		};

		TextureLoader.SetButtonTextures(wet, "world_setup.wet");
		wet.Pressed += () => {
			clim = WorldCharacteristics.Climate.Wet;
			ResetClimateGraphics();
			wetLarge.Visible = true;
		};

		TextureLoader.SetButtonTextures(warm, "world_setup.warm");
		warm.Pressed += () => {
			temp = WorldCharacteristics.Temperature.Warm;
			ResetTemperatureGraphics();
			warmLarge.Visible = true;
		};

		TextureLoader.SetButtonTextures(temperate, "world_setup.temperate");
		temperate.Pressed += () => {
			temp = WorldCharacteristics.Temperature.Temperate;
			ResetTemperatureGraphics();
			temperateLarge.Visible = true;
		};

		TextureLoader.SetButtonTextures(cool, "world_setup.cool");
		cool.Pressed += () => {
			temp = WorldCharacteristics.Temperature.Cool;
			ResetTemperatureGraphics();
			coolLarge.Visible = true;
		};

		TextureLoader.SetButtonTextures(billion3, "world_setup.billion3");
		billion3.Pressed += () => {
			age = WorldCharacteristics.Age.Billion_3;
			ResetAgeGraphics();
			billion3Large.Visible = true;
		};

		TextureLoader.SetButtonTextures(billion4, "world_setup.billion4");
		billion4.Pressed += () => {
			age = WorldCharacteristics.Age.Billion_4;
			ResetAgeGraphics();
			billion4Large.Visible = true;
		};

		TextureLoader.SetButtonTextures(billion5, "world_setup.billion5");
		billion5.Pressed += () => {
			age = WorldCharacteristics.Age.Billion_5;
			ResetAgeGraphics();
			billion5Large.Visible = true;
		};

		TextureLoader.SetButtonTextures(confirm, "ui.confirm");
		confirm.Pressed += CreateGame;

		TextureLoader.SetButtonTextures(cancel, "ui.cancel");
		cancel.Pressed += BackToMainMenu;

		pangaea60Large.Texture = TextureLoader.Load("world_setup.large.pangaea60");
		pangaea70Large.Texture = TextureLoader.Load("world_setup.large.pangaea70");
		pangaea80Large.Texture = TextureLoader.Load("world_setup.large.pangaea80");
		continents60Large.Texture = TextureLoader.Load("world_setup.large.continents60");
		continents70Large.Texture = TextureLoader.Load("world_setup.large.continents70");
		continents80Large.Texture = TextureLoader.Load("world_setup.large.continents80");
		archipelago60Large.Texture = TextureLoader.Load("world_setup.large.archipelago60");
		archipelago70Large.Texture = TextureLoader.Load("world_setup.large.archipelago70");
		archipelago80Large.Texture = TextureLoader.Load("world_setup.large.archipelago80");

		aridLarge.Texture = TextureLoader.Load("world_setup.large.arid");
		normalLarge.Texture = TextureLoader.Load("world_setup.large.normal");
		wetLarge.Texture = TextureLoader.Load("world_setup.large.wet");
		coolLarge.Texture = TextureLoader.Load("world_setup.large.cool");
		temperateLarge.Texture = TextureLoader.Load("world_setup.large.temperate");
		warmLarge.Texture = TextureLoader.Load("world_setup.large.warm");
		billion3Large.Texture = TextureLoader.Load("world_setup.large.billion3");
		billion4Large.Texture = TextureLoader.Load("world_setup.large.billion4");
		billion5Large.Texture = TextureLoader.Load("world_setup.large.billion5");

		ResetLandformGraphics();
		pangaea70Large.Visible = true;
		pangaea70.ButtonPressed = true;
		pangaeaLabel.Text = "Pangaea (70% water)";

		ResetClimateGraphics();
		normalLarge.Visible = true;
		normal.ButtonPressed = true;

		ResetTemperatureGraphics();
		temperateLarge.Visible = true;
		temperate.ButtonPressed = true;

		ResetAgeGraphics();
		billion4Large.Visible = true;
		billion4.ButtonPressed = true;

		// TODO: handle different map sizes properly (including loading the
		// optimal city number, etc)
		tinySize.Visible = false;
		smallSize.Visible = false;
		standardSize.Visible = false;
		largeSize.Visible = false;
		hugeSize.Visible = false;
		randomSize.Visible = false;
	}

	private void ResetLandformGraphics() {
		pangaea60Large.Visible = false;
		pangaea70Large.Visible = false;
		pangaea80Large.Visible = false;
		continents60Large.Visible = false;
		continents70Large.Visible = false;
		continents80Large.Visible = false;
		archipelago60Large.Visible = false;
		archipelago70Large.Visible = false;
		archipelago80Large.Visible = false;

		pangaeaLabel.Text = "Pangaea";
		continentsLabel.Text = "Continents";
		archipelagoLabel.Text = "Archipelago";
	}

	private void ResetClimateGraphics() {
		aridLarge.Visible = false;
		normalLarge.Visible = false;
		wetLarge.Visible = false;
	}

	private void ResetTemperatureGraphics() {
		coolLarge.Visible = false;
		temperateLarge.Visible = false;
		warmLarge.Visible = false;
	}

	private void ResetAgeGraphics() {
		billion3Large.Visible = false;
		billion4Large.Visible = false;
		billion5Large.Visible = false;
	}

	private void CreateGame() {
		GlobalSingleton Global = GetNode<GlobalSingleton>("/root/GlobalSingleton");
		Global.ResetLoadGameFields();
		SaveGame save = GameModeLoader.Load(GamePaths.GameModesDir, GamePaths.GameMode);

		Global.WorldCharacteristics = new WorldCharacteristics() {
			landform = landform,
			oceanCoverage = ocean,
			age = age,
			climate = clim,
			temperature = temp,
			worldSize = new WorldSize() {
				width = 100,
				height = 100,
				numberOfCivs = 8,
				distanceBetweenCivs = 12,
				techRate = 240,
				optimalNumberOfCities = 20,
			},
			terrainTypes = save.TerrainTypes,
			resources = save.Resources,
			defaultGovernment = save.Governments.Find(x => x.defaultType),
			mapSeed = Int32.Parse(seedInput.Text),
			maxRankOfWorkableTiles = save.Rules.MaxRankOfWorkableTiles,
			maxRankOfBarbarianCampTiles = save.Rules.MaxRankOfBarbarianCampTiles,
		};

		Global.SaveGame = save;

		GetTree().ChangeSceneToFile("res://UIElements/NewGame/player_setup.tscn");
	}

	private void BackToMainMenu() {
		GetTree().ChangeSceneToFile("res://UIElements/MainMenu/main_menu.tscn");
	}
}
