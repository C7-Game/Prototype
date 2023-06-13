using Godot;
using ConvertCiv3Media;
using System;

public partial class UnitControlButton : TextureButton {

	public string action; // corresponding Godot action (keybinding)
	private int x;
	private int y;
	private Action<string> onPressedAction;

	public static int scale = 32; // how many pixels each button is in each direction

	public UnitControlButton(string action, int x, int y, Action<string> onPressedAction) {
		this.action = action;
		this.x = x;
		this.y = y;
		this.onPressedAction = onPressedAction;
	}

	// Called when the node enters the scene tree for the first time.
	public override void _Ready() {
		Pcx buttonPcx = new Pcx(Util.Civ3MediaPath("Conquests/Art/interface/NormButtons.PCX"));
		Pcx buttonPcxRollover = new Pcx(Util.Civ3MediaPath("Conquests/Art/interface/rolloverbuttons.PCX"));
		Pcx buttonPcxPressed = new Pcx(Util.Civ3MediaPath("Conquests/Art/interface/highlightedbuttons.PCX"));
		Pcx buttonPcxAlpha = new Pcx(Util.Civ3MediaPath("Conquests/Art/interface/ButtonAlpha.pcx"));
		ImageTexture menuTexture = PCXToGodot.getImageFromPCXWithAlphaBlend(buttonPcx, buttonPcxAlpha, x * scale, y * scale, scale, scale);
		ImageTexture rolloverTexture = PCXToGodot.getImageFromPCXWithAlphaBlend(buttonPcxRollover, buttonPcxAlpha, x * scale, y * scale, scale, scale);
		ImageTexture pressedTexture = PCXToGodot.getImageFromPCXWithAlphaBlend(buttonPcxPressed, buttonPcxAlpha, x * scale, y * scale, scale, scale);
		this.TextureNormal = menuTexture;
		this.TextureHover = rolloverTexture;
		this.TexturePressed = pressedTexture;

		this.Connect("pressed", new Callable(this, "onButtonPress"));
	}

	private void onButtonPress() {
		onPressedAction(this.action);
	}
}
