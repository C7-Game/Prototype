# C7 Game Data

This namespace is intended to store the game data.  There will be some top level object, and various sub-objects, e.g. units, maps, cities, etc.

The current thought is the UI will interact with the C7 Engine, and the C7 Engine will update, or read from, the C7 Game Data as needed.  This Game Data would thus be the master copy of the game state.

This may be tweaked in the future, but this is a starting point that might be sustainable, and as we're increasingly getting to the point where we need some data, why not take a stab at it?