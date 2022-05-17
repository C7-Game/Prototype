namespace C7Engine
{
	using C7GameData;

	public static class TileExtensions {
		public static MapUnit FindTopDefender(this Tile tile, MapUnit opponent)
		{
			if (tile.unitsOnTile.Count > 0) {
				MapUnit tr = tile.unitsOnTile[0];
				foreach (MapUnit u in tile.unitsOnTile)
					if (u.HasPriorityAsDefender(tr, opponent))
						tr = u;
				return tr;
			} else
				return MapUnit.NONE;
		}
	}
}
