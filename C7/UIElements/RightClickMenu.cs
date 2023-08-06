using System.Collections.Generic;
using Godot;
using C7GameData;
using C7Engine;

public partial class RightClickMenu : VBoxContainer {
	protected Game game;

	protected RightClickMenu(Game game) : base() {
		this.game = game;

		// Set theme for menu node. TODO: This should be made moddable. I noticed in the Godot docs something about loading themes from files
		// but didn't look into how it works, but that's probably what we'll want to do.
		Color black = Color.Color8(0, 0, 0, 255);
		var theme = new Theme();
		theme.SetConstant("separation", "VBoxContainer", 0);
		theme.SetColor("font_color", "Button", black);
		theme.SetColor("font_color_hover", "Button", black);
		theme.SetColor("font_color_pressed", "Button", black);
		theme.SetColor("font_color_focus", "Button", black);
		theme.SetStylebox("normal", "Button", GetItemStyleBox(Color.Color8(255, 247, 222, 255)));
		theme.SetStylebox("hover", "Button", GetItemStyleBox(Color.Color8(255, 189, 107, 255)));
		theme.SetStylebox("pressed", "Button", GetItemStyleBox(Color.Color8(140, 200, 200, 255)));
		this.Theme = theme;

		this.Hide();

		// Add the menu as a child of "CanvasLayer" to ensure it's drawn overtop of the game map. "CanvasLayer" should have been named
		// something like "UILayer" since it's the layer that contains all UI elements. "CanvasLayer" is actually its type.
		game.GetNode("CanvasLayer").AddChild(this);
	}

	public void Open(Vector2 position) {
		// Must show the container first in order to update its RectSize
		this.Show();

		// Move "position" if the menu would extend past the right or bottom edges of the screen
		Vector2 offScreen = position + this.Size - DisplayServer.WindowGetSize();
		if (offScreen.X > 0) {
			position.X = Mathf.Max(0, position.X - offScreen.X);
		}
		if (offScreen.Y > 0) {
			position.Y = Mathf.Max(0, position.Y - offScreen.Y);
		}
		this.SetPosition(position);
	}

	public void CloseAndDelete() {
		this.QueueFree();
	}

	private static StyleBoxFlat GetItemStyleBox(Color color) {
		return new StyleBoxFlat() {
			BgColor = color,
			ContentMarginLeft = 4f,
			ContentMarginTop = 2f,
			ContentMarginRight = 4f,
			ContentMarginBottom = 2f
		};
	}

	public void AddItem(string text, System.Action action, Texture2D icon = null) {
		Button button = new Button();
		button.Text = text;
		if (icon != null) {
			button.Icon = icon;
		}
		button.Alignment = HorizontalAlignment.Left;
		button.Connect("pressed", Callable.From(action));
		this.AddChild(button);
	}

	public void RemoveAll() {
		foreach (Node child in this.GetChildren()) {
			child.QueueFree();
		}
	}

	public override void _Input(InputEvent @event) {
		bool mouseOverMenu = new Rect2(Vector2.Zero, this.Size).HasPoint(this.GetLocalMousePosition());
		bool escapeKeyWasPressed = (@event is InputEventKey keyEvent) && keyEvent.Pressed && keyEvent.Keycode == Godot.Key.Escape;
		bool mouseClickedOutsideMenu = (@event is InputEventMouseButton mouseButtonEvent) && mouseButtonEvent.IsPressed() && !mouseOverMenu;

		if (escapeKeyWasPressed || mouseClickedOutsideMenu) {
			this.AcceptEvent(); // Prevents other controls from receiving this event
			CloseAndDelete();
			// Eat all events other than mouse events while the cursor is over the menu. We want the menu to grab all input while it's open but we
			// must make sure not to block mouse events from reaching its child buttons. (This had me confused for a while since the Godot docs
			// say that events reach children before their parents, but the catch is that there are three phases of input processing. The "input"
			// phase, this function, then "gui input", and finally "unhandled input". If a control eats an event during the "input" phase it won't
			// proceed to the "gui input" phase where buttons actually respond to it.)
		} else if (!((@event is InputEventMouse) && mouseOverMenu)) {
			this.AcceptEvent();
		}
	}
}

public partial class RightClickTileMenu : RightClickMenu {
	public RightClickTileMenu(Game game, Tile tile) : base(game) {
		ResetItems(tile);
	}

	private bool isUnitFortified(MapUnit unit, Dictionary<ID, bool> uiStates) {
		if (uiStates is null || !uiStates.ContainsKey(unit.id)) {
			return unit.isFortified;
		}
		return uiStates[unit.id];
	}

	private string getUnitAction(MapUnit unit, bool isFortified) {
		if (unit.owner == game.controller) {
			return isFortified ? "Wake" : "Activate";
		}
		return "Contact";
	}

	// uiUpdatedUnitStates maps unit guid to a boolean that is true if they were fortified
	// and false if they were selected in the previous action. This is to update the UI
	// since the actions update the engine asynchronously and otherwise the UI may not
	// reflect these changes immediately.
	public void ResetItems(Tile tile, Dictionary<ID, bool> uiUpdatedUnitStates = null) {
		RemoveAll();

		int fortifiedCount = 0;
		List<MapUnit> units = tile.unitsOnTile.FindAll(unit => unit.owner.guid == game.controller.guid);

		foreach (MapUnit unit in units) {
			bool isFortified = isUnitFortified(unit, uiUpdatedUnitStates);
			fortifiedCount += isFortified ? 1 : 0;
			string actionName = getUnitAction(unit, isFortified);
			AddItem($"{actionName} {unit.Describe()}", () => SelectUnit(unit.id));
		}
		int unfortifiedCount = units.Count - fortifiedCount;

		if (fortifiedCount > 1) {
			AddItem($"Wake All ({fortifiedCount} units)", () => ForAll(tile.xCoordinate, tile.yCoordinate, false));
		}
		if (unfortifiedCount > 1) {
			AddItem($"Fortify All ({unfortifiedCount} units)", () => ForAll(tile.xCoordinate, tile.yCoordinate, true));
		}
	}

	public void SelectUnit(ID id) {
		using (var gameDataAccess = new UIGameDataAccess()) {
			MapUnit toSelect = gameDataAccess.gameData.mapUnits.Find(u => u.id == id);
			if (toSelect != null && toSelect.owner == game.controller) {
				game.setSelectedUnit(toSelect);
				new MsgSetFortification(toSelect.id, false).send();
				ResetItems(toSelect.location, new Dictionary<ID, bool>() { { toSelect.id, false } });
			}
		}
		if (!Input.IsKeyPressed(Godot.Key.Shift)) {
			CloseAndDelete();
		}
	}

	public void ForAll(int tileX, int tileY, bool isFortify) {
		using (var gameDataAccess = new UIGameDataAccess()) {
			bool hasSelectedUnit = false;
			Tile tile = gameDataAccess.gameData.map.tileAt(tileX, tileY);
			Dictionary<ID, bool> modified = new Dictionary<ID, bool>();
			foreach (MapUnit unit in tile.unitsOnTile) {
				if (unit.isFortified != isFortify) {
					modified[unit.id] = isFortify;
					new MsgSetFortification(unit.id, isFortify).send();

					if (!hasSelectedUnit && !isFortify) {
						game.setSelectedUnit(unit);
					}
				}
			}
			ResetItems(tile, modified);
		}
		if (!Input.IsKeyPressed(Godot.Key.Shift)) {
			CloseAndDelete();
		}
	}

}

public partial class RightClickChooseProductionMenu : RightClickMenu {
	private ID cityID;

	private ImageTexture GetProducibleIcon(IProducible producible) {
		if (producible is UnitPrototype proto) {
			const int iconWidth = 32, iconHeight = 32, iconsPerRow = 14;
			int x = 1 + 33 * (proto.iconIndex % iconsPerRow),
				y = 1 + 33 * (proto.iconIndex / iconsPerRow);
			return Util.LoadTextureFromPCX("Art/Units/units_32.pcx", x, y, iconWidth, iconHeight);
		} else
			return null;
	}

	public RightClickChooseProductionMenu(Game game, City city) : base(game) {
		cityID = city.id;
		foreach (IProducible option in city.ListProductionOptions()) {
			int buildTime = city.TurnsToProduce(option);
			AddItem($"{option.name} ({buildTime} turns)", () => ChooseProduction(option.name), GetProducibleIcon(option));
		}
	}

	public void ChooseProduction(string producibleName) {
		new MsgChooseProduction(cityID, producibleName).send();
		CloseAndDelete();
	}
}
