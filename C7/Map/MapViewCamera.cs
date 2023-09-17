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
		private int wrapEdgeTileMargin = 2; // margin in number of tiles to trigger map wrapping
		private HorizontalWrapState hwrap = HorizontalWrapState.None;

		public async void attachToMapView(MapView map) {
			this.map = map;
			wrapEdgeMargin = wrapEdgeTileMargin * map.tileSize.X;
			map.updateAnimations();

			// Awaiting a 0 second timer is a workaround to force GlobalCanvasTransform to be updated.
			// This is necessary when the camera's starting position is close to the edge of the map.
			// Without it, the GlobalCanvasTransform will not be updated until the camera is moved,
			// resulting in broken map wrapping. I tried awaiting "process_frame" but it does not seem
			// to work, although I believe that is what we want to do here. GPT-4 suggested waiting for
			// a 0 second timer, which seems to work.
			await ToSignal(GetTree().CreateTimer(0), "timeout");
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
		private float wrapEdgeMargin = 0;
		float wrapRightEdge {get => map.worldEdgeRight - wrapEdgeMargin; }
		float wrapLeftEdge {get => map.worldEdgeLeft + wrapEdgeMargin; }
		private bool enteringRightWrap(Rect2 v) => hwrap != HorizontalWrapState.Right && v.End.X >= wrapRightEdge;
		private bool enteringLeftWrap(Rect2 v) => hwrap != HorizontalWrapState.Left && v.Position.X <= wrapLeftEdge;
		private bool atEdgeOfRightWrap(Rect2 v) => hwrap == HorizontalWrapState.Right && v.End.X >= wrapRightEdge + map.pixelWidth;
		private bool atEdgeOfLeftWrap(Rect2 v) => hwrap == HorizontalWrapState.Left && v.Position.X <= wrapLeftEdge - map.pixelWidth;
		private HorizontalWrapState currentHwrap(Rect2 v) {
			return v.Position.X <= wrapLeftEdge ? HorizontalWrapState.Left : (v.End.X >= wrapRightEdge ? HorizontalWrapState.Right : HorizontalWrapState.None);
		}

		// checkWorldWrap determines if the camera is about to spill over the world map and will:
		// - move the second "wrap" tilemap to the appropriate edge
		//   to give the illusion of true wrapping tilemap
		// - teleport the camera one world-width to the left or right when
		//   only the "wrap" tilemap is in view
		private void checkWorldWrap() {
			if (map is null || !map.wrapHorizontally) {
				// TODO: for maps that do not wrap horizontally restrict movement
				return;
			}
			Rect2 visibleWorld = getVisibleWorld();
			if (enteringRightWrap(visibleWorld)) {
				map.setHorizontalWrap(HorizontalWrapState.Right);
			} else if (enteringLeftWrap(visibleWorld)) {
				map.setHorizontalWrap(HorizontalWrapState.Left);
			}
			if (atEdgeOfRightWrap(visibleWorld)) {
				Translate(Vector2.Left * map.pixelWidth);
			} else if (atEdgeOfLeftWrap(visibleWorld)) {
				Translate(Vector2.Right * map.pixelWidth);
			}
			hwrap = currentHwrap(visibleWorld);
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

		private Transform2D viewportToGlobalTransform => (GetViewport().GlobalCanvasTransform * GetCanvasTransform()).AffineInverse();

		public Rect2 getVisibleWorld() => viewportToGlobalTransform * GetViewportRect();

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
