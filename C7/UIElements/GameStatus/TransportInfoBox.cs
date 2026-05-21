using System;
using System.Collections.Generic;
using System.Linq;
using C7.Textures;
using Godot;
using C7GameData;
using Serilog;
using C7Engine;

[GlobalClass]
[Tool]
public partial class TransportInfoBox : Civ3TextureRect {
	private ILogger log = LogManager.ForContext<TransportInfoBox>();

	private readonly Game _game;

	private Vector2I frameOffset = new (-27, -180);
	private Vector2 transportUnitsAnchor = new(70f, 45f);
	private Vector2 miniatureScale = new(0.7f, 0.7f);
	private Vector2 unitButtonSize = new(50, 50);
	// Note: the draw area for the box is a bit more than 200x100

	private TextureRect boxTransportRect = new();

	private Dictionary<ID, bool> unitTracker = new();



	public TransportInfoBox(Game game) {
		_game = game;

		MouseFilter = MouseFilterEnum.Stop;
	}

	public override void _Ready() {
		ImageTexture boxTransport = TextureLoader.Load("transport_infobox.box");

		boxTransportRect = new TextureRect();
		boxTransportRect.Texture = boxTransport;
		boxTransportRect.SetPosition(new Vector2(0, 0));
		AddChild(boxTransportRect);
	}

	public override void _Process(double delta) {
		if (Engine.IsEditorHint())
			return;

		RepositionFrame();

		EngineStorage.ReadGameData((GameData gD) => {
			var unit = _game.CurrentlySelectedUnit;
			if (unit == null || unit == MapUnit.NONE || !unit.CanTransport()) {
				Visible = false;
				ClearUnitSprites();
				return;
			}

			if (!unitTracker.TryGetValue(unit.id, out _))
				ClearUnitSprites();

			Visible = true;
			var loadedUnits = gD.mapUnits.Where(u => u.IsLoadedIn(unit));
			var transportUnits = new List<MapUnit>([unit]).Concat(loadedUnits).ToList();

			UpdateUnitGraphic(transportUnits);
		});

		base._Process(delta);
	}

	private void RepositionFrame() {
		// Position frame and map relative to viewport
		var boxSize = boxTransportRect.Texture.GetSize();
		var vp = GetViewportRect().Size;
		SetPosition(frameOffset + new Vector2(vp.X - boxSize.X, vp.Y - boxSize.Y));
	}

	private void ClearUnitSprites() {
		unitTracker.Clear();
		foreach (var c in GetChildren().Where(c => c is TextureButton))
			c.QueueFree();
	}

	private void UpdateUnitGraphic(ICollection<MapUnit> units) {
		if (!units.Any(u => u != MapUnit.NONE && u != null)) {
			return;
		}

		// Wait for game to load unit graphics
		if (!AnimationManager.AnimationThumbnails.Any())
			return;

		foreach (var (unit, idx) in units.Select((x, i) => (x, i))) {
			if (unitTracker.TryGetValue(unit.id, out _)) {
				continue;
			}

			// Get sprites
			var (unitSprite, unitTintSprite) = SpriteUtils.GetUnitSprites(_game, unit);
			unitTracker[unit.id] = true;

			// Resize sprites
			unitSprite.SetScale(miniatureScale);
			unitTintSprite.SetScale(miniatureScale);

			// Create button
			TextureButton unitButton = new();
			unitButton.SetSize(unitButtonSize);
			unitButton.Pressed += () => HandleBoxClick(unit);
			AddChild(unitButton);

			// Position button
			var pos = CalculateUnitButtonPosition(idx);
			unitButton.SetPosition(pos);

			// Add sprites
			unitButton.AddChild(unitSprite);
			unitButton.AddChild(unitTintSprite);

			// Draw a box around the transport unit
			if (idx == 0) {
				var line = new Line2D();

				line.Width = 3f;
				line.DefaultColor = TextureLoader.LoadColor(unit.owner.GetPlayerColor());
				line.Position = unitButtonSize / -2f;

				// draw lines at normal scale, let parent scale things down 
				line.AddPoint(new Vector2(0, 0));
				line.AddPoint(new Vector2(unitButtonSize.X, 0));
				line.AddPoint(new Vector2(unitButtonSize.X, unitButtonSize.Y));
				line.AddPoint(new Vector2(0, unitButtonSize.Y));
				line.AddPoint(new Vector2(0, 0));

				// parent to button
				unitButton.AddChild(line);
			}
		}
	}

	private Vector2 CalculateUnitButtonPosition(int idx) {
		// TODO: common baseline

		var drawAreaWidth = boxTransportRect.Texture.GetWidth() - transportUnitsAnchor.X;
		var columns = (int) Math.Floor(drawAreaWidth / unitButtonSize.X);

		var unitSpritePosition = transportUnitsAnchor;

		unitSpritePosition.X += (idx % columns) * unitButtonSize.X;
		unitSpritePosition.Y += ((int)Math.Floor(idx / (1f * columns))) * unitButtonSize.Y;

		return unitSpritePosition;
	}

	private void HandleBoxClick(MapUnit unit) {
		Log.Information("Unit Box click: {MapUnit}", unit);
		// TODO: Select unit based on click inside TransportInfoBox
		// EmitSignal(SignalName.CenterCameraOnActiveUnit);
	}
}
