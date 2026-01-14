using Godot;

[GlobalClass]
[Tool]
public partial class Civ3TextureRect : TextureRect {
	[Export] string textureConfigKey;

	public override void _Ready() {
		if (textureConfigKey != null) {
			Texture = TextureLoader.Load(textureConfigKey);
		}
	}

	public override void _ValidateProperty(Godot.Collections.Dictionary property) {
		Util.ApplyNoSaveFlag(property, [PropertyName.Texture]);
	}
}
