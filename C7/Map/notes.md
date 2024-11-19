# state
currently the new MapView is stateful, for example when a city is built:

build city in ui -> msg to engine -> city added to GameData -> msg to ui -> Game calls MapView.addCity

This is hard to manage because Game must make the correct calls into MapView to ensure MapView is in sync with GameData. This pattern of updating MapView is also wherein lies the performance boost, for example when a road is built: MapView calculates what road texture to use, and it is added to TileMap. This is only done once when the road is built, then Godot manages drawing the TileMap contents instead of doing it every frame in c#.

I think the current city approach will become too complex as more elements must be rendered, and will introduce bugs where MapView does not reflect GameData. Potential compromises:

1. When GameData changes (the game map changes), recalculate everything in MapView - expensive, but not every frame. This may be worse than the non-TileMap implementation because updating TileMap might not be cheap...
2. When GameData changes, determine all tiles that could possibly have changed appearance (ie. creating or destroying a road changes the appearance of that tile and 0 or more of its neighbors visually, so recalculate everything for those 9 tiles) - this is roughly speaking what updateTile currently does for all things rendered in the TileMap (currently everything except for cities, units, and terrain which has its own TileMap instance).

# wrapping
the easiest way to implement wrapping is to have 2 copies of the tilemap and translate the second copy to wherever the camera is "hanging" over the edge of the main tilemap. Depending on how expensive this is (mainly in memory consumption? need to profile) a better approach may be to split the TileMap in half (left/right for horizontally wrapping maps, top/bottom for vertically) or in four (corners for maps that wrap both ways) and move them around to cover the entire screen. neither approach accounts for the edge case where the camera is zoomed out far enough / display large enough that tiles are visible multiple times on screen.
