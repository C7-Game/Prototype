using Godot;
using System;
using ConvertCiv3Media;
using C7GameData;

public partial class TestUnit : Node2D
{

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		AnimationManager manager = new AnimationManager(null);
		UnitSprite sprite = new UnitSprite(manager);
		UnitPrototype prototype = new UnitPrototype{name="warrior"};
		manager.forUnit(prototype, MapUnit.AnimatedAction.RUN).loadSpriteAnimation();
		string name = AnimationManager.AnimationKey(prototype, MapUnit.AnimatedAction.RUN, TileDirection.EAST);
		AddChild(sprite);

		float scale = 6;
		this.Scale = new Vector2(scale, scale);

		sprite.material.SetShaderParameter("tintColor", new Vector3(1f,1f,1f));
		sprite.Position = new Vector2(30, 30);
		sprite.Play(name);

		AnimatedSprite2D cursor = new AnimatedSprite2D();
		SpriteFrames cursorFrames = new SpriteFrames();
		cursor.SpriteFrames = cursorFrames;
		AnimationManager.loadCursorAnimation("Art/Animations/Cursor/Cursor.flc", ref cursorFrames);
		cursor.Position = new Vector2(120, 30);
		cursor.Play("cursor");
		AddChild(cursor);
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}
}
