using System.Collections.Generic;
using System.Linq;
using C7Engine;
using Godot;
using C7Engine.PalaceMinigame;

[Tool]
public partial class PalaceScreen : Civ3TextureRect {
	[Export] HBoxContainer switchButtonContainer;
	ButtonGroup switchButtonGroup = new();

	string activeCulture;
	List<Building> assignedBuildings = [];
	Building pendingBuilding;

	Dictionary<string, Culture> cultures = [];

	public override void _Ready() {
		base._Ready();

		if (C7Settings.UseStandaloneMode()) {
			return;
		}

		string configPath = Util.Civ3MediaPath("Text/PalaceView.txt");
		ConfigParser parser = new();

		cultures = parser.Parse(configPath);
		activeCulture = cultures.Keys.First();

		foreach (Culture culture in cultures.Values) {
			AddSwitchButton(culture);
		}
	}

	public override void _Process(double delta) {
		if (Engine.IsEditorHint()) return;
		QueueRedraw();
	}

	public override void _Draw() {
		foreach (Building b in assignedBuildings.OrderBy(b => b.Index)) {
			ImageTexture texture = TextureLoader.LoadByPath(b.TexturePath);
			DrawTexture(texture, new Vector2(b.X, b.Y));
		}

		if (pendingBuilding != null) {
			ImageTexture texture = TextureLoader.LoadByPath(pendingBuilding.TexturePath);
			DrawTexture(texture, new Vector2(pendingBuilding.X, pendingBuilding.Y), new Color(1, 1, 1, 0.45f));
		}
	}

	public override void _GuiInput(InputEvent @event) {
		if (@event is InputEventMouseButton eventMouseButton) {
			if (pendingBuilding == null) return;

			if (eventMouseButton.ButtonIndex == MouseButton.Left && eventMouseButton.Pressed) {
				assignedBuildings.Add(pendingBuilding);
				pendingBuilding = null;
			}
		} else if (@event is InputEventMouseMotion eventMouseMotion) {
			foreach (Building building in AvailableBuildings()) {
				ImageTexture texture = TextureLoader.LoadByPath(building.TexturePath);
				Rect2 textureRect = new() {
					Position = new(building.X, building.Y),
					Size = texture.GetSize()
				};

				if (textureRect.HasPoint(eventMouseMotion.Position)) {
					pendingBuilding = building;
					return;
				}
			}

			pendingBuilding = null;
		}
	}

	private IEnumerable<Building> AvailableBuildings() {
		var assignedIndexes = assignedBuildings.Select(b=> b.Index);

		return cultures[activeCulture].Buildings
				.Where(b => !assignedIndexes.Contains(b.Index))
				.Where(b => b.Prerequisites.All(index => assignedIndexes.Contains(index)));
	}

	private void AddSwitchButton(Culture culture) {
		var bt = culture.ButtonTextures;

		TextureButton button = new() {
			TextureNormal = TextureLoader.LoadByPath(bt.Normal),
			TexturePressed = TextureLoader.LoadByPath(bt.Pressed),
			TextureHover = TextureLoader.LoadByPath(bt.Hover),
			ButtonGroup = switchButtonGroup,
			ToggleMode = true,
		};
		button.Pressed += () => { activeCulture = culture.Name; };

		if (culture.Name == activeCulture) button.ButtonPressed = true;

		switchButtonContainer.AddChild(button);
	}
}
