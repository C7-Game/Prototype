using Godot;

[GlobalClass]
[Tool]
public partial class Civ3TextureButton : TextureButton {
	[Export] string textureConfigKey;

	public override void _Ready() {
		if (textureConfigKey != null) {
			TextureLoader.SetButtonTextures(this, textureConfigKey);
		}
	}

	public override void _ValidateProperty(Godot.Collections.Dictionary property) {
		Util.ApplyNoSaveFlag(property, [PropertyName.TextureNormal, PropertyName.TextureHover, PropertyName.TexturePressed]);
	}
}
