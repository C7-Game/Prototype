using System;
using C7GameData;
using Godot;

public partial class MiniMapControls : Control {
	public override void _Ready() {
		MouseFilter = MouseFilterEnum.Stop;
	}

	/// Render the viewport bounds as a rectangle on the minimap image
	public void DrawBounds(Image mapImage, GameMap map, MapView.VisibleRegion vr) {
		// TODO: Handle draws over map edges, maybe with Mathf.Wrap

		// Bounds, in minimap draw space
		var maxWidth = map.numTilesWide - 1;
		var maxHeight = (map.numTilesTall / 2) - 1;
		var maxPan = 100; // how many screenfuls one can pan the map

		// Wrapped coordinates, working around modulo operator limitations
		var wax = (maxPan * maxWidth + vr.upperLeftX) % maxWidth;
		var way = (maxPan * maxHeight + (vr.upperLeftY / 2)) % maxHeight;
		var wbx = (maxPan * maxWidth + vr.lowerRightX) % maxWidth;
		var wby = (maxPan * maxHeight + (vr.lowerRightY / 2)) % maxHeight;

		// Out of bounds
		var isOoBx = wbx < wax; // X coordinates increase right
		var isOoBy = wby < way; // Y coordinates increase down
		var fullZoom = (vr.lowerRightY / 2 - vr.upperLeftY / 2) > maxHeight
					   || (vr.lowerRightX - vr.upperLeftX) > maxWidth;

		// TODO: make use of GameMap's wrapHorizontally, wrapVertically

		// Handle the four cases
		if (fullZoom)
			DrawBoundsRegular(mapImage, 0, 0, maxWidth, maxHeight);
		else if (isOoBx && isOoBy)
			DrawBoundsOverCorner(mapImage, wax, way, wbx, wby, maxWidth, maxHeight);
		else if (isOoBx)
			DrawBoundsOverSideEdges(mapImage, wax, way, wbx, wby, maxWidth, maxHeight);
		else if (isOoBy)
			DrawBoundsOverPoles(mapImage, wax, way, wbx, wby, maxWidth, maxHeight);
		else
			DrawBoundsRegular(mapImage, wax, way, wbx, wby);
	}

	private void DrawBoundsOverCorner(Image mapImage, int wax, int way, int wbx, int wby, int maxWidth, int maxHeight) {
		// Top Left
		DrawLineOnImage(mapImage, 0, wby, wbx, wby, Colors.White); // bottom
		DrawLineOnImage(mapImage, wbx, wby, wbx, 0, Colors.White); // right

		// Top Right
		DrawLineOnImage(mapImage, wax, 0, wax, wby, Colors.White); // left
		DrawLineOnImage(mapImage, wax, wby, maxWidth, wby, Colors.White); // bottom

		// Bottom Left 
		DrawLineOnImage(mapImage, 0, way, wbx, way, Colors.White); // top
		DrawLineOnImage(mapImage, wbx, way, wbx, maxHeight, Colors.White); // right

		// Bottom Right
		DrawLineOnImage(mapImage, wax, maxHeight, wax, way, Colors.White); // left
		DrawLineOnImage(mapImage, wax, way, maxWidth, way, Colors.White); // top
	}

	private void DrawBoundsOverSideEdges(Image mapImage, int wax, int way, int wbx, int wby, int maxWidth, int maxHeight) {
		// Left half
		DrawLineOnImage(mapImage, 0, way, wbx, way, Colors.White); // top
		DrawLineOnImage(mapImage, wbx, way, wbx, wby, Colors.White); // right
		DrawLineOnImage(mapImage, 0, wby, wbx, wby, Colors.White); // bottom

		// Right half
		DrawLineOnImage(mapImage, maxWidth, way, wax, way, Colors.White); // top
		DrawLineOnImage(mapImage, wax, way, wax, wby, Colors.White); // left
		DrawLineOnImage(mapImage, wax, wby, maxWidth, wby, Colors.White); // bottom		
	}

	private void DrawBoundsOverPoles(Image mapImage, int wax, int way, int wbx, int wby, int maxWidth, int maxHeight) {
		// Top half
		DrawLineOnImage(mapImage, wax, 0, wax, wby, Colors.White); // left
		DrawLineOnImage(mapImage, wax, wby, wbx, wby, Colors.White); // bottom
		DrawLineOnImage(mapImage, wbx, wby, wbx, 0, Colors.White); // right

		// Bottom half
		DrawLineOnImage(mapImage, wax, maxHeight, wax, way, Colors.White); // left
		DrawLineOnImage(mapImage, wax, way, wbx, way, Colors.White); // top
		DrawLineOnImage(mapImage, wbx, way, wbx, maxHeight, Colors.White); // right
	}

	private void DrawBoundsRegular(Image mapImage, int wax, int way, int wbx, int wby) {
		DrawLineOnImage(mapImage, wax, way, wax, wby, Colors.White); // left
		DrawLineOnImage(mapImage, wax, wby, wbx, wby, Colors.White); // bottom
		DrawLineOnImage(mapImage, wbx, wby, wbx, way, Colors.White); // right
		DrawLineOnImage(mapImage, wbx, way, wax, way, Colors.White); // top
	}

	/// <summary>
	/// Draw a line on an Image using the SetPixel primitive.<br/>
	/// https://en.wikipedia.org/wiki/Bresenham%27s_line_algorithm
	/// </summary>
	private void DrawLineOnImage(Image image, int x0, int y0, int x1, int y1, Color color) {
		int dx = Math.Abs(x1 - x0);
		int dy = Math.Abs(y1 - y0);
		int sx = x0 < x1 ? 1 : -1;
		int sy = y0 < y1 ? 1 : -1;
		int err = dx - dy;

		while (true) {
			image.SetPixel(x0, y0, color);
			if (x0 == x1 && y0 == y1) break;
			int e2 = err * 2;
			if (e2 > -dy) { err -= dy; x0 += sx; }
			if (e2 < dx) { err += dx; y0 += sy; }
		}
	}
}
