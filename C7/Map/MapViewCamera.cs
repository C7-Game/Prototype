using Godot;
using C7GameData;

namespace C7.Map {

	// MapViewCamera position and zoom should only be modified through the
	// following methods:
	// - scaleZoom
	// - setZoom
	// - setPosition
	// This is because these methods will ensure that MapViewCamera handles
	// world wrapping on the MapView automatically.
	public partial class MapViewCamera : Camera2D {
		private readonly float maxZoom = 3.0f;
		private readonly float minZoom = 0.2f;
		public float zoomFactor { get; private set; } = 1.0f;
		private MapView map;
		private int wrapLeeway = 2; // leeway in number of tiles to consider camera at map edge
		private HorizontalWrapState hwrap = HorizontalWrapState.None;

		public void attachToMapView(MapView map) {
			this.map = map;
			worldWrapLeeway = wrapLeeway * map.tileSize.X;
			map.updateAnimations();
			checkWorldWrap();
		}

		public override void _Ready() {
			base._Ready();
			scaleZoom(zoomFactor);
		}

		public void scaleZoom(float factor) {
			zoomFactor = zoomFactor * factor;
			zoomFactor = Mathf.Clamp(zoomFactor, minZoom, maxZoom);
			Zoom = Vector2.One * zoomFactor;
			checkWorldWrap();
		}

		public void setZoom(float factor) {
			zoomFactor = Mathf.Clamp(factor, minZoom, maxZoom);
			Zoom = Vector2.One * zoomFactor;
			checkWorldWrap();
		}

		public void setPosition(Vector2 position) {
			Position = position;
			checkWorldWrap();
		}
		private int worldWrapLeeway = 0;

		private bool enteringRightWrap(Rect2 v) => hwrap != HorizontalWrapState.Right && v.End.X >= map.worldEdgeRight - worldWrapLeeway;
		private bool enteringLeftWrap(Rect2 v) => hwrap != HorizontalWrapState.Left && v.Position.X <= map.worldEdgeLeft + worldWrapLeeway;
		private bool atEdgeOfRightWrap(Rect2 v) => hwrap == HorizontalWrapState.Right && v.End.X >= map.worldEdgeRight + map.pixelWidth;
		private bool atEdgeOfLeftWrap(Rect2 v) => hwrap == HorizontalWrapState.Left && v.Position.X <= map.worldEdgeLeft - map.pixelWidth;
		private HorizontalWrapState currentHwrap(Rect2 v) {
			return v.Position.X <= map.worldEdgeLeft + worldWrapLeeway ? HorizontalWrapState.Left : (v.End.X >= map.worldEdgeRight - worldWrapLeeway ? HorizontalWrapState.Right : HorizontalWrapState.None);
		}

		private void checkWorldWrap() {
			if (map is null || !map.wrapHorizontally) {
				// TODO: for maps that do not wrap horizontally restrict movement
				return;
			}
			Rect2 visibleWorld = getVisibleWorld();
			if (enteringRightWrap(visibleWorld)) {
				GD.Print("moving wrap to right");
				map.setHorizontalWrap(HorizontalWrapState.Right);
			} else if (enteringLeftWrap(visibleWorld)) {
				GD.Print("moving wrap to left");
				map.setHorizontalWrap(HorizontalWrapState.Left);
			}
			if (atEdgeOfRightWrap(visibleWorld)) {
				Translate(Vector2.Left * map.pixelWidth);
			} else if (atEdgeOfLeftWrap(visibleWorld)) {
				Translate(Vector2.Right * map.pixelWidth);
			}
			hwrap = currentHwrap(visibleWorld);
		}

		public override void _Process(double delta) {
			checkWorldWrap();
		}

		public override void _UnhandledInput(InputEvent @event) {
			switch (@event) {
				case InputEventMouseMotion mouseDrag when mouseDrag.ButtonMask == MouseButtonMask.Left:
					setPosition(Position - mouseDrag.Relative / Zoom);
					break;
				case InputEventMagnifyGesture magnifyGesture:
					scaleZoom(magnifyGesture.Factor);
					break;
			}
		}

		public Rect2 getVisibleWorld() {
			Transform2D vpToGlobal = (GetViewport().GlobalCanvasTransform * GetCanvasTransform()).AffineInverse();
			return vpToGlobal * GetViewportRect();
		}

		public void centerOnTile(Tile tile, MapView map) {
			Vector2 target = map.tileToLocal(tile);
			setPosition(target);
		}

		public bool isTileInView(Tile tile, MapView map) {
			Rect2 visible = getVisibleWorld();
			Vector2 target = map.tileToLocal(tile);
			float size = 30;
			target -= Vector2.One * (size / 2);
			Rect2 boundingBox = new Rect2(target, size, size);
			return visible.Encloses(boundingBox);
		}
	}
}
