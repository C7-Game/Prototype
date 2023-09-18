namespace C7GameData.Save {
	public struct TileLocation {
		public int x, y;
		public TileLocation(int x, int y) {
			this.x = x;
			this.y = y;
		}
		public TileLocation(Tile tile) {
			x = tile.xCoordinate;
			y = tile.yCoordinate;
		}
		public TileLocation() {
			x = -1;
			y = -1;
		}
	}
}
