using Godot;

public static class LabelExtensions {
	public static void SetTextAndCenterLabel(this Label label, string text) {
		//For the centered labels, we anchor them center, with equal weight on each side.
		//Then, when they are visible, we add a left margin that's negative and equal to half
		//their width.
		//Seems like there probably is an easier way, but I haven't found it yet.
		label.Text = text;
		label.HorizontalAlignment = HorizontalAlignment.Center;
		label.AnchorLeft = 0.5f;
		label.AnchorRight = 0.5f;
		label.OffsetLeft = -1 * (label.Size.X / 2.0f);
	}
}
