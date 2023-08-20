using Godot;
using C7GameData;

public partial class TestUnit : Node2D
{
	public override void _Ready()
	{
		AnimationManager manager = new AnimationManager(null);
		UnitSprite sprite = new UnitSprite(manager);
		UnitPrototype prototype = new UnitPrototype{name="warrior"};
		manager.forUnit(prototype, MapUnit.AnimatedAction.RUN).loadSpriteAnimation();
		string name = AnimationManager.AnimationKey(prototype, MapUnit.AnimatedAction.RUN, TileDirection.EAST);
		AddChild(sprite);
		sprite.Play(name);

		float scale = 6;
		this.Scale = new Vector2(scale, scale);

		sprite.SetColor(new Color(1, 1, 1));
		sprite.Position = new Vector2(30, 30);

		CursorSprite cursor = new CursorSprite();
		cursor.Position = new Vector2(120, 30);
		AddChild(cursor);
	}
}
