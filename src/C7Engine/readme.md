# C7 Engine

The C7 Engine is the core gameplay mechanic part of the code base.  The UI (in the C7 folder) will call the engine when the player takes actions, and the engine will update the game state and perhaps send a reply back (e.g. a result of combat).  The C7 Engine will interact with the C7 Game Data, which will store the state of the game.

This should hopefully keep the UI and the engine and the data somewhat decoupled, and facilitate both maintenance and exploring networking options.

## Layout

I've added a folder, EntryPoints, which is intended to be where we put all the methods that can be invoked in the engine by the game.  I'm anticipating that the engine might become slightly complex someday, and having all the places you can call the engine be separate from all the helper methods and deep calculations it performs could be useful.