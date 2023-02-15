using Godot;
using System;
using ConvertCiv3Media;

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

	public (ShaderMaterial, MeshInstance2D) shadedQuadFromFlicFrame(string shaderPath, ImageTexture palette, ImageTexture indices) {
		var shader = GD.Load<Shader>(shaderPath);
		var (mat, mesh) = createShadedQuad(shader);
		mat.SetShaderParameter("palette", palette);
		mat.SetShaderParameter("indices", indices);
		var (civColorWhitePalette, _) = Util.loadPalettizedPCX("Art/Units/Palettes/ntp00.pcx");
		mat.SetShaderParameter("civColorWhitePalette", civColorWhitePalette);
		return (mat, mesh);
	}

	public Util.FlicSheet flicSheet(Flic flic) {
		var texPalette = Util.createPaletteTexture(flic.Palette);

		var countColumns = flic.Images.GetLength(1); // Each column contains one frame
		var countRows = flic.Images.GetLength(0); // Each row contains one animation
		var countImages = countColumns * countRows;

		byte[] allIndices = new byte[countRows * countColumns * flic.Width * flic.Height];
		// row, col loop over the sprites, each one a frame of the animation
		for (int row = 0; row < countRows; row++)
			for (int col = 0; col < countColumns; col++)
				// x, y loop over pixels within each sprite
				for (int y = 0; y < flic.Height; y++)
					for (int x = 0; x < flic.Width; x++) {
						int pixelRow = row * flic.Height + y,
							pixelCol = col * flic.Width + x,
							pixelIndex = pixelRow * countColumns * flic.Width + pixelCol;
						allIndices[pixelIndex] = flic.Images[row, col][y * flic.Width + x];
					}

		var imgIndices = Image.CreateFromData(countColumns * flic.Width, countRows * flic.Height, false, Image.Format.R8, allIndices);
		ImageTexture texIndices = ImageTexture.CreateFromImage(imgIndices);

		return new Util.FlicSheet { palette = texPalette, indices = texIndices, spriteWidth = flic.Width, spriteHeight = flic.Height };
	}

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		//AudioStreamPlayer player = GetNode<AudioStreamPlayer>("CanvasLayer/SoundEffectPlayer");
		// var ad = new Civ3AnimData(null);
		// var anim = ad.forUnit("Warrior", C7GameData.MapUnit.AnimatedAction.DEFAULT);
		// var flicSheet = anim.getFlicSheet();
		// var inst = new AnimationInstance(this, flicSheet);

		AnimatedSprite2D sprite = new AnimatedSprite2D();
		SpriteFrames frames = new SpriteFrames();
		sprite.SpriteFrames = frames;
		Util.loadFlicAnimation("Art/Units/warrior/warriorRun.flc", "run", ref frames);

		AnimatedSprite2D spriteTint = new AnimatedSprite2D();
		SpriteFrames framesTint = new SpriteFrames();
		spriteTint.SpriteFrames = framesTint;
		Util.loadFlicAnimation("Art/Units/warrior/warriorRun.flc", "run", ref framesTint);

		ShaderMaterial material = new ShaderMaterial();
		material.Shader = GD.Load<Shader>("res://tests/Tint.gdshader");
		material.SetShaderParameter("tintColor", new Vector3(1f,1f,1f));
		spriteTint.Material = material;

		AddChild(sprite);
		AddChild(spriteTint);

		sprite.Play("run_EAST");
		spriteTint.Play("TINT_run_EAST");
		sprite.Position = new Vector2(30, 30);
		spriteTint.Position = new Vector2(80, 30);

		// var flic = Util.LoadFlic("Art/Units/warrior/warriorDefault.flc");
		// var sheet = flicSheet(flic);

		// var (mat, mesh) = shadedQuadFromFlicFrame("res://tests/Anim.gdshader", sheet.palette, sheet.indices);
		// mat.SetShaderParameter("civColor", new Vector3(1, 1, 1));

		// this.AddChild(mesh);

		// var tex = Util.LoadTextureFromFlicData(flic.Images[0, 0], flic.Palette, flic.Width, flic.Height);
		// var tr = new TextureRect();
		// tr.Texture = tex;
		// // this.AddChild(tr);
		// // tr.Show();

		float SCALE = 8;
		this.Scale = new Vector2(SCALE, SCALE);
		// this.Position = new Vector2(30, 30);

		// AnimatedSprite2D sprite = new AnimatedSprite2D();
		// sprite.SpriteFrames = new SpriteFrames();
		// sprite.SpriteFrames.AddAnimation("run");
		// foreach (byte[] image in flic.Images) {
		// 	tex = Util.LoadTextureFromFlicData(image, flic.Palette, flic.Width, flic.Height);
		// 	sprite.SpriteFrames.AddFrame("run", tex, 0.5f); // duration is in unit ini
		// }
		// sprite.Position = new Vector2(30, 30);

		// ShaderMaterial material = new ShaderMaterial();
		// sprite.Material = material;

		// material.Shader = GD.Load<Shader>("res://tests/Anim.gdshader");

		// this.AddChild(sprite);
		// sprite.Show();
		// sprite.Play("run");

		// inst.shaderMat.SetShaderParameter("civColor", new Vector3(1, 1, 1));
		// // inst.meshInst.Position = new Vector2(50, 50);
		// inst.meshInst.Scale = new Vector2(flicSheet.spriteWidth, -1 * flicSheet.spriteHeight);
		// inst.meshInst.ZIndex = 100;
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}
}
