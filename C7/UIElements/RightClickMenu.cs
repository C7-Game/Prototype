using Godot;
using C7GameData;
using C7Engine;

public class RightClickMenu : VBoxContainer
{
	protected Game game;

	protected RightClickMenu(Game game) : base()
	{
		this.game = game;

		// Set theme for menu node. TODO: This should be made moddable. I noticed in the Godot docs something about loading themes from files
		// but didn't look into how it works, but that's probably what we'll want to do.
		Color black = Color.Color8(0, 0, 0, 255);
		var theme = new Theme();
		theme.SetConstant("separation", "VBoxContainer", 0);
		theme.SetColor("font_color"        , "Button", black);
		theme.SetColor("font_color_hover"  , "Button", black);
		theme.SetColor("font_color_pressed", "Button", black);
		theme.SetColor("font_color_focus"  , "Button", black);
		theme.SetStylebox("normal" , "Button", GetItemStyleBox(Color.Color8(255, 247, 222, 255)));
		theme.SetStylebox("hover"  , "Button", GetItemStyleBox(Color.Color8(255, 189, 107, 255)));
		theme.SetStylebox("pressed", "Button", GetItemStyleBox(Color.Color8(140, 200, 200, 255)));
		this.Theme = theme;

		this.Hide();

		// Add the menu as a child of "CanvasLayer" to ensure it's drawn overtop of the game map. "CanvasLayer" should have been named
		// something like "UILayer" since it's the layer that contains all UI elements. "CanvasLayer" is actually its type.
		game.GetNode("CanvasLayer").AddChild(this);
	}

	public void Open(Vector2 position)
	{
		this.RectPosition = position;
		this.Show();
	}

	public void CloseAndDelete()
	{
		this.QueueFree();
	}

	private static StyleBoxFlat GetItemStyleBox(Color color)
	{
		var styleBox = new StyleBoxFlat();
		styleBox.BgColor = color;
		styleBox.ContentMarginLeft   = 4f;
		styleBox.ContentMarginTop    = 2f;
		styleBox.ContentMarginRight  = 4f;
		styleBox.ContentMarginBottom = 2f;
		return styleBox;
	}

	public Button AddItem(string text, Texture icon = null)
	{
		var button = new Button();
		button.Text = text;
		if (icon != null)
			button.Icon = icon;
		button.Align = Button.TextAlign.Left;
		this.AddChild(button);
		return button;
	}

	public override void _Input(InputEvent @event)
	{
		bool mouseOverMenu = new Rect2(Vector2.Zero, this.RectSize).HasPoint(this.GetLocalMousePosition()),
		     escapeKeyWasPressed = (@event is InputEventKey keyEvent) && keyEvent.Pressed && keyEvent.Scancode == (int)Godot.KeyList.Escape,
		     mouseClickedOutsideMenu = (@event is InputEventMouseButton mouseButtonEvent) && mouseButtonEvent.IsPressed() && ! mouseOverMenu;

		if (escapeKeyWasPressed || mouseClickedOutsideMenu) {
			this.AcceptEvent(); // Prevents other controls from receiving this event
			CloseAndDelete();

		// Eat all events other than mouse events while the cursor is over the menu. We want the menu to grab all input while it's open but we
		// must make sure not to block mouse events from reaching its child buttons. (This had me confused for a while since the Godot docs
		// say that events reach children before their parents, but the catch is that there are three phases of input processing. The "input"
		// phase, this function, then "gui input", and finally "unhandled input". If a control eats an event during the "input" phase it won't
		// proceed to the "gui input" phase where buttons actually respond to it.)
		} else if (! ((@event is InputEventMouse) && mouseOverMenu))
			this.AcceptEvent();
	}
}

public class RightClickTileMenu : RightClickMenu
{
	public RightClickTileMenu(Game game, Tile tile) : base(game)
	{
		foreach (MapUnit unit in tile.unitsOnTile) {
			string action = (unit.owner == game.controller) ?
				(unit.isFortified ? "Wake" : "Activate") :
				"Contact";
			AddItem($"{action} {unit.Describe()}").Connect("pressed", this, "SelectUnit", new Godot.Collections.Array() {unit.guid});
		}
	}

	public void SelectUnit(string guid)
	{
		using (var gameDataAccess = new UIGameDataAccess()) {
			MapUnit toSelect = gameDataAccess.gameData.mapUnits.Find(u => u.guid == guid);
			if (toSelect != null && toSelect.owner == game.controller) {
				game.setSelectedUnit(toSelect);
				new MsgSetFortification(toSelect.guid, false).send();
			}
		}
		CloseAndDelete();
	}
}

public class RightClickChooseProductionMenu : RightClickMenu
{
	private string cityGUID;

	public ImageTexture GetProducibleIcon(IProducible producible)
	{
		if (producible is UnitPrototype proto) {
			const int iconWidth = 32, iconHeight = 32, iconsPerRow = 14;
			int x = 1 + 33 * (proto.iconIndex % iconsPerRow),
			    y = 1 + 33 * (proto.iconIndex / iconsPerRow);
			return Util.LoadTextureFromPCX("Art/Units/units_32.pcx", x, y, iconWidth, iconHeight);
		} else
			return null;
	}

	public RightClickChooseProductionMenu(Game game, City city) : base(game)
	{
		cityGUID = city.guid;
		foreach (IProducible option in city.ListProductionOptions()) {
			int buildTime = city.TurnsToProduce(option);
			AddItem($"{option.name} ({buildTime} turns)", GetProducibleIcon(option))
				.Connect("pressed", this, "ChooseProduction", new Godot.Collections.Array() { option.name });
		}
	}

	public void ChooseProduction(string producibleName)
	{
		new MsgChooseProduction(cityGUID, producibleName).send();
		CloseAndDelete();
	}
}
