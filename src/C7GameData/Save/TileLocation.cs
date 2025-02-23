namespace C7GameData.Save {
	public struct TileLocation {
		public int X, Y;
		public TileLocation(int X, int Y) {
			this.X = X;
			this.Y = Y;
		}
		public TileLocation(Tile tile) {
			X = tile.XCoordinate;
			Y = tile.YCoordinate;
		}
		public TileLocation() {
			X = -1;
			Y = -1;
		}
	}
}
