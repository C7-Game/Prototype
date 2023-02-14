using Godot;
using System;

public partial class TestUnit : Node2D
{

	public static (ShaderMaterial, MeshInstance2D) createShadedQuad(Shader shader)
	{
		PlaneMesh mesh = new PlaneMesh();
		mesh.Size = new Vector2(1, 1);
		mesh.SubdivideDepth = 0;
		mesh.Orientation = PlaneMesh.OrientationEnum.Z;

		ShaderMaterial shaderMat = new ShaderMaterial();
		shaderMat.Shader = shader;

		MeshInstance2D meshInst = new MeshInstance2D();
		meshInst.Material = shaderMat;
		meshInst.Mesh = mesh;

		return (shaderMat, meshInst);
	}

	public partial class AnimationInstance {
		public ShaderMaterial shaderMat;
		public MeshInstance2D meshInst;

		private void init(ShaderMaterial mat, MeshInstance2D mesh, Util.FlicSheet flicSheet) {
			mat.SetShaderParameter("palette", flicSheet.palette);
			mat.SetShaderParameter("indices", flicSheet.indices);

			var indicesDims = new Vector2(flicSheet.indices.GetWidth(), flicSheet.indices.GetHeight());
			var spriteSize = new Vector2(flicSheet.spriteWidth, flicSheet.spriteHeight);
			mat.SetShaderParameter("relSpriteSize", spriteSize / indicesDims);

			mat.SetShaderParameter("spriteXY", new Vector2(0, 0));
		}

		public AnimationInstance(Node2D parent, Util.FlicSheet flicSheet)
		{
			var shader = GD.Load<Shader>("res://tests/Unit.gdshader");
			(shaderMat, meshInst) = createShadedQuad(shader);
			init(shaderMat, meshInst, flicSheet);
			var (civColorWhitePalette, _) = Util.loadPalettizedPCX("Art/Units/Palettes/ntp00.pcx");
			shaderMat.SetShaderParameter("civColorWhitePalette", civColorWhitePalette);
			parent.AddChild(meshInst);
			meshInst.Position = new Vector2(120, 30);
		}
	}

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		//AudioStreamPlayer player = GetNode<AudioStreamPlayer>("CanvasLayer/SoundEffectPlayer");
		var ad = new Civ3AnimData(null);
		var anim = ad.forUnit("Warrior", C7GameData.MapUnit.AnimatedAction.DEFAULT);
		var flicSheet = anim.getFlicSheet();
		var inst = new AnimationInstance(this, flicSheet);

		var flic = Util.LoadFlic("Art/Units/warrior/warriorAttackA.flc");
		var tex = Util.LoadTextureFromFlicData(flic.Images[0, 0], flic.Palette, flic.Width, flic.Height);

		var tr = new TextureRect();
		tr.Texture = tex;
		// this.AddChild(tr);
		// tr.Show();
		this.Scale = new Vector2(8, 8);
		this.Position = new Vector2(30, 30);

		AnimatedSprite2D sprite = new AnimatedSprite2D();
		sprite.SpriteFrames = new SpriteFrames();
		sprite.SpriteFrames.AddAnimation("run");
		foreach (byte[] image in flic.Images) {
			tex = Util.LoadTextureFromFlicData(image, flic.Palette, flic.Width, flic.Height);
			sprite.SpriteFrames.AddFrame("run", tex, 0.5f); // duration is in unit ini
		}
		sprite.Position = new Vector2(30, 30);

		this.AddChild(sprite);
		sprite.Show();
		sprite.Play("run");

		inst.shaderMat.SetShaderParameter("civColor", new Vector3(1, 1, 1));
		inst.meshInst.Position = new Vector2(50, 50);
		inst.meshInst.Scale = new Vector2(flicSheet.spriteWidth, -1 * flicSheet.spriteHeight);
		inst.meshInst.ZIndex = 100;
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}
}
