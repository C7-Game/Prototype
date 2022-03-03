using Godot;
using C7GameData;
using C7Engine;

public class RightClickMenu : VBoxContainer
{
	private Game game;

	public RightClickMenu(Game game) : base()
	{
		this.game = game;
	}

	private static StyleBoxFlat GetMenuItemStyleBox(Color color)
	{
		var styleBox = new StyleBoxFlat();
		styleBox.BgColor = color;
		styleBox.ContentMarginLeft   = 4f;
		styleBox.ContentMarginTop    = 2f;
		styleBox.ContentMarginRight  = 4f;
		styleBox.ContentMarginBottom = 2f;
		return styleBox;
	}

	public static RightClickMenu OpenForTile(Game game, Vector2 position, Tile tile)
	{
		var rCM = new RightClickMenu(game);

		// Set theme for menu node. TODO: This should be made moddable. I noticed in the Godot docs something about loading themes from files
		// but didn't look into how it works, but that's probably what we'll want to do.
		Color black = Color.Color8(0, 0, 0, 255);
		var theme = new Theme();
		theme.SetConstant("separation", "VBoxContainer", 0);
		theme.SetColor("font_color"        , "Button", black);
		theme.SetColor("font_color_hover"  , "Button", black);
		theme.SetColor("font_color_pressed", "Button", black);
		theme.SetColor("font_color_focus"  , "Button", black);
		theme.SetStylebox("normal" , "Button", GetMenuItemStyleBox(Color.Color8(255, 247, 222, 255)));
		theme.SetStylebox("hover"  , "Button", GetMenuItemStyleBox(Color.Color8(255, 189, 107, 255)));
		theme.SetStylebox("pressed", "Button", GetMenuItemStyleBox(Color.Color8(140, 200, 200, 255)));
		rCM.Theme = theme;

		foreach (MapUnit unit in tile.unitsOnTile) {
			var button = new Button();
			button.Text = unit.unitType.name;
			button.Align = Button.TextAlign.Left;
			button.Connect("pressed", rCM, "SelectUnit", new Godot.Collections.Array() {unit.guid});
			rCM.AddChild(button);
		}

		rCM.RectPosition = position;
		game.AddChild(rCM);
		return rCM;
	}

	public void SelectUnit(string guid)
	{
		using (var gameDataAccess = new UIGameDataAccess()) {
			MapUnit toSelect = gameDataAccess.gameData.mapUnits.Find(u => u.guid == guid);
			if (toSelect != null && toSelect.owner == game.controller)
				game.setSelectedUnit(toSelect);
		}
		this.QueueFree(); // Closes and deletes the menu
	}

	public override void _Input(InputEvent @event)
	{
		bool mouseOverMenu = new Rect2(Vector2.Zero, this.RectSize).HasPoint(this.GetLocalMousePosition()),
		     escapeKeyWasPressed = (@event is InputEventKey keyEvent) && keyEvent.Pressed && keyEvent.Scancode == (int)Godot.KeyList.Escape,
		     mouseClickedOutsideMenu = (@event is InputEventMouseButton mouseButtonEvent) && mouseButtonEvent.IsPressed() && ! mouseOverMenu;

		if (escapeKeyWasPressed || mouseClickedOutsideMenu) {
			this.QueueFree(); // Closes and deletes the menu
			this.AcceptEvent(); // Prevents other controls from receiving this event

		// Eat all events other than mouse events while the cursor is over the menu. We want the menu to grab all input while it's open but we
		// must make sure not to block mouse events from reaching its child buttons. (This had me confused for a while since the Godot docs
		// say that events reach children before their parents, but the catch is that there are three phases of input processing. The "input"
		// phase, this function, then "gui input", and finally "unhandled input". If a control eats an event during the "input" phase it won't
		// proceed to the "gui input" phase where buttons actually respond to it.)
		} else if (! ((@event is InputEventMouse) && mouseOverMenu))
			this.AcceptEvent();
	}
}
