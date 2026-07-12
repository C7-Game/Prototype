using System.Collections.Generic;
using System.Linq;
using C7Engine.PalaceMinigame;
using Godot;

[Tool]
public partial class PalaceBuildingsLayer : Control {
	string activeCulture;
	Dictionary<string, Culture> cultures = [];

	Building pendingBuilding;
	List<Building> assignedBuildings = [];

	public override void _Ready() {
		MouseFilter = MouseFilterEnum.Stop;
		SetAnchorsPreset(LayoutPreset.FullRect);
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

	public void SetCultures(Dictionary<string, Culture> culturesMap) {
		cultures = culturesMap;
		activeCulture = culturesMap.Keys.First();
	}

	public void ActivateCulture(Culture culture) {
		activeCulture = culture.Name;
	}
}
