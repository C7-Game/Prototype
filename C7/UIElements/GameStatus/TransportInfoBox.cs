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

	private Vector2 transportUnitsAnchor = new Vector2(70f, 45f);

	private TextureRect boxTransportRect = new();
	private TextureButton boxTransportRectButton = new();

	private Label unitRank = new();
	private Label unitType = new();

	private Dictionary<ID, Tuple<Sprite2D, Sprite2D>> unitSpritesCache = new();

	private float extraXOffset = 7;
	private Vector2I frameOffset = new (-20, -175);

	public TransportInfoBox(Game game) {
		_game = game;
	}

	public override void _Ready() {
		ImageTexture boxTransport = TextureLoader.Load("transport_infobox.box");

		boxTransportRect = new TextureRect();
		boxTransportRect.Texture = boxTransport;
		boxTransportRect.SetPosition(new Vector2(0, 0));
		AddChild(boxTransportRect);

		// // An "invisible" button covering the inside area of the box so we can register the click
		// // and center the camera on the unit or end the turn
		// boxTransportRectButton.SetSize(new Vector2(228, 108));
		// boxTransportRectButton.SetPosition(new Vector2(40, 17));
		// AddChild(boxTransportRectButton);
		// boxTransportRectButton.Pressed += HandleBoxClick;
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
		unitSpritesCache.Clear();
		foreach (var c in GetChildren().Where(c => c is Sprite2D))
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
			if (unitSpritesCache.TryGetValue(unit.id, out var sprites)) {
				continue;
			}

			(var unitSprite, var unitTintSprite) = StatusUtils.GetUnitSprites(_game, unit);
			unitSpritesCache[unit.id] = new Tuple<Sprite2D, Sprite2D>(unitSprite, unitTintSprite);

			var unitSpritePosition = transportUnitsAnchor;
			var unitSpriteDrawWidth = boxTransportRect.Texture.GetWidth() - transportUnitsAnchor.X;

			unitSpritePosition.X += idx * unitSprite.Texture.GetWidth();

			while (unitSpritePosition.X > unitSpriteDrawWidth) {
				unitSpritePosition.X -= unitSpriteDrawWidth;
				unitSpritePosition.Y += unitSprite.Texture.GetHeight();
			}

			unitSprite.Position = unitSpritePosition;
			AddChild(unitSprite);
			unitTintSprite.Position = unitSpritePosition;
			AddChild(unitTintSprite);

			// unitSprite.Pressed += HandleBoxClick; // TODO: this won't work
		}
	}



	private void HandleBoxClick() {
		// EmitSignal(SignalName.CenterCameraOnActiveUnit);
	}
}
