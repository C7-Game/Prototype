using Godot;

/**
 * A work-around to not being able to pass objects in Godot.
 * To pass via signals, an object must extend Godot.Object.
 * This class allows us to work around that limitation fairly
 * easily.
 * Taken from https://github.com/godotengine/godot/issues/16706#issuecomment-394605337
 * Example sending:
 *     ParameterWrapper<MapUnit> wrappedUnit = new ParameterWrapper<MapUnit>(SelectedUnit);
 *     EmitSignal(nameof(NewAutoselectedUnit), wrappedUnit);
 * Example receiving:
 *    public void OnNewUnitSelected(ParameterWrapper<MapUnit> mapUnitThing) {
 *        MapUnit unwrappedUnit = mapUnitThing.Value;
 *        // Do whatever you like with unwrappedUnit...
 *    }
 **/

public partial class ParameterWrapper<T> : RefCounted {
	public T Value { get; private set; }

	public ParameterWrapper(T value) {
		Value = value;
	}
}
