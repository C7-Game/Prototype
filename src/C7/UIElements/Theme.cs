using Godot;

public static class ThemeFactory {

	static ThemeFactory() {
		defaultTheme = new Theme();
		defaultTheme.SetColor("caret_color", "LineEdit", Colors.Black);
	}

	private static Theme defaultTheme;

	public static Theme DefaultTheme {
		get => defaultTheme;
	}
}
