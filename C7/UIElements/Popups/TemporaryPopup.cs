using Godot;
using System;
using System.Threading.Tasks;

public partial class TemporaryPopup : Label {
	private int durationInMillis;

	public TemporaryPopup(string text, float durationInSec) {
		Text = text;
		this.durationInMillis = (int)(durationInSec * 1000);

		AddThemeStyleboxOverride("normal", PopupStyleBox());
		AddThemeColorOverride("font_color", Colors.White);
	}

	// A stylebox that works well for temporary popups or tooltips in game.
	public static StyleBoxFlat PopupStyleBox() {
		StyleBoxFlat styleBox = new();
		styleBox.ContentMarginLeft = 5;
		styleBox.ContentMarginRight = 5;
		styleBox.ContentMarginTop = 2;
		styleBox.ContentMarginBottom = 2;
		styleBox.BgColor = Color.FromHtml("1C1C1C");
		return styleBox;
	}

	public static StyleBoxFlat PopupTechStyleBox() {
		StyleBoxFlat styleBox = new();
		styleBox.ContentMarginLeft = 5;
		styleBox.ContentMarginRight = 5;
		styleBox.ContentMarginTop = 2;
		styleBox.ContentMarginBottom = 2;
		styleBox.BorderColor = Colors.Black;
		styleBox.BorderWidthBottom = 1;
		styleBox.BorderWidthTop = 1;
		styleBox.BorderWidthLeft = 1;
		styleBox.BorderWidthRight = 1;
		styleBox.BgColor = Color.FromHtml("F8F8F8");
		return styleBox;
	}

	public async void ShowPopup() {
		// Wait until we hit our duration then destroy ourself.
		await Task.Delay(durationInMillis);

		Visible = false;
		QueueFree();
	}
}
