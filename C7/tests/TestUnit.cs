using Godot;
using System;
using ConvertCiv3Media;

public partial class TestUnit : Node2D
{

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		//AudioStreamPlayer player = GetNode<AudioStreamPlayer>("CanvasLayer/SoundEffectPlayer");

		AnimatedSprite2D sprite = new AnimatedSprite2D();
		SpriteFrames frames = new SpriteFrames();
		sprite.SpriteFrames = frames;

		AnimatedSprite2D spriteTint = new AnimatedSprite2D();
		SpriteFrames framesTint = new SpriteFrames();
		spriteTint.SpriteFrames = framesTint;

		AnimationManager.loadFlicAnimation("Art/Units/warrior/warriorRun.flc", "run", ref frames, ref framesTint);

		ShaderMaterial material = new ShaderMaterial();
		material.Shader = GD.Load<Shader>("res://UnitTint.gdshader");
		material.SetShaderParameter("tintColor", new Vector3(1f,1f,1f));
		spriteTint.Material = material;

		AddChild(sprite);
		AddChild(spriteTint);

		sprite.Play("run_EAST");
		spriteTint.Play("run_EAST");
		sprite.Position = new Vector2(30, 30);
		spriteTint.Position = new Vector2(30, 30);

		float SCALE = 6;
		this.Scale = new Vector2(SCALE, SCALE);


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
