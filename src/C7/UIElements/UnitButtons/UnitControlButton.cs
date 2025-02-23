using Godot;
using ConvertCiv3Media;
using System;

public partial class UnitControlButton : TextureButton {

	public string action; // corresponding Godot action (keybinding)
	private int X;
	private int Y;
	private Action<string> onPressedAction;

	public static int scale = 32; // how many pixels each button is in each direction

	public UnitControlButton(string action, int X, int Y, Action<string> onPressedAction) {
		this.action = action;
		this.X = X;
		this.Y = Y;
		this.onPressedAction = onPressedAction;
	}

	// Called when the node enters the scene tree for the first time.
	public override void _Ready() {
		Pcx buttonPcx = new Pcx(Util.Civ3MediaPath("Conquests/Art/interface/NormButtons.PCX"));
		Pcx buttonPcxRollover = new Pcx(Util.Civ3MediaPath("Conquests/Art/interface/rolloverbuttons.PCX"));
		Pcx buttonPcxPressed = new Pcx(Util.Civ3MediaPath("Conquests/Art/interface/highlightedbuttons.PCX"));
		Pcx buttonPcxAlpha = new Pcx(Util.Civ3MediaPath("Conquests/Art/interface/ButtonAlpha.pcx"));
		ImageTexture menuTexture = PCXToGodot.getImageFromPCXWithAlphaBlend(buttonPcx, buttonPcxAlpha, X * scale, Y * scale, scale, scale);
		ImageTexture rolloverTexture = PCXToGodot.getImageFromPCXWithAlphaBlend(buttonPcxRollover, buttonPcxAlpha, X * scale, Y * scale, scale, scale);
		ImageTexture pressedTexture = PCXToGodot.getImageFromPCXWithAlphaBlend(buttonPcxPressed, buttonPcxAlpha, X * scale, Y * scale, scale, scale);
		this.TextureNormal = menuTexture;
		this.TextureHover = rolloverTexture;
		this.TexturePressed = pressedTexture;

		this.Pressed += onButtonPress;
	}

	private void onButtonPress() {
		onPressedAction(this.action);
	}
}
