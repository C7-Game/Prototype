namespace C7GameData {
	// The strings for each action correspond to values in project.godot for keyboard shortcuts
	public static class C7Action {
		public const string EndTurn = "end_turn";
		public const string Escape = "escape";
		public const string MoveUnitSouthwest = "move_unit_southwest";
		public const string MoveUnitSouth = "move_unit_south";
		public const string MoveUnitSoutheast = "move_unit_southeast";
		public const string MoveUnitWest = "move_unit_west";
		public const string MoveUnitEast = "move_unit_east";
		public const string MoveUnitNorthwest = "move_unit_northwest";
		public const string MoveUnitNorth = "move_unit_north";
		public const string MoveUnitNortheast = "move_unit_northeast";
		public const string ToggleAnimations = "toggle_animations";
		public const string ToggleGrid = "toggle_grid";
		public const string ToggleZoom = "toggle_zoom";
		public const string UnitBombard = "unit_bombard";
		public const string UnitBuildCity = "unit_build_city";
		public const string UnitBuildRoad = "unit_build_road";
		public const string UnitBuildMine = "unit_build_mine";
		public const string UnitIrrigate = "unit_irrigate";
		public const string UnitDisband = "unit_disband";
		public const string UnitExplore = "unit_explore";
		public const string UnitFortify = "unit_fortify";
		public const string UnitGoto = "unit_goto";
		public const string UnitHold = "unit_hold";
		public const string UnitSentry = "unit_sentry";
		public const string UnitSentryEnemyOnly = "unit_sentry_enemy_only";
		public const string UnitWait = "unit_wait";

		// This method transforms an action string into a TileDirection.
		// A null value will be returned if the conversion is unsuccessful.
		public static TileDirection? ToTileDirection(string action) {
			return action switch {
				MoveUnitSouthwest => TileDirection.SOUTHWEST,
				MoveUnitSouth => TileDirection.SOUTH,
				MoveUnitSoutheast => TileDirection.SOUTHEAST,
				MoveUnitWest => TileDirection.WEST,
				MoveUnitEast => TileDirection.EAST,
				MoveUnitNorthwest => TileDirection.NORTHWEST,
				MoveUnitNorth => TileDirection.NORTH,
				MoveUnitNortheast => TileDirection.NORTHEAST,
				_ => null,
			};
		}
	}
}
