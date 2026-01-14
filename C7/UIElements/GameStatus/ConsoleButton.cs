using Godot;
using System;

/// <summary>
/// ConsoleButton is a custom control node that displays a textured button with a
/// label. During gameplay the button is hidden by default.
///
/// This control is specifically made to be used inside a BoxContainer. Even
/// when the button is hidden, the ConsoleButton node maintains its size to avoid
/// triggering layout recalculations.
///
/// The button's tooltip and label text are customizable via exports.
/// </summary>
[GlobalClass]
[Tool]
public partial class ConsoleButton : Control {
	[Export] string text;
	[Export] string tooltipText;

	[Signal] public delegate void PressedEventHandler();

	private TextureButton button;

	public override void _Ready() {
		button = new() {
			TextureNormal = TextureLoader.Load("ui.console.normal"),
			TextureHover = TextureLoader.Load("ui.console.hover"),
			TexturePressed = TextureLoader.Load("ui.console.pressed"),
			TooltipText = tooltipText
		};
		button.Pressed += () => { EmitSignal(SignalName.Pressed); };

		Label label = new() {
			Text = text,
			OffsetLeft = 3,
			OffsetTop = -3,
			MouseFilter = MouseFilterEnum.Ignore
		};

		AddChild(button);
		button.AddChild(label);

		CustomMinimumSize = button.Size;

		// During the gameplay the button should be hidden by default
		if (!Engine.IsEditorHint()) {
			button.Hide();
		}
	}

	public void ShowButton() {
		button.Show();
	}
}
