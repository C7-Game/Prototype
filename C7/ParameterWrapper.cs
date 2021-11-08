/**
 * A work-around to not being able to pass objects in Godot.
 * To pass via signals, an object must extend Godot.Object.
 * This class allows us to work around that limitation fairly
 * easily.
 * Taken from https://github.com/godotengine/godot/issues/16706#issuecomment-394605337
 * Example sending:
 * 		ParameterWrapper wrappedUnit = new ParameterWrapper(SelectedUnit);
 *      EmitSignal(nameof(NewAutoselectedUnit), wrappedUnit);
 * Example receiving:
 *      public void OnNewUnitSelected(ParameterWrapper mapUnitThing) {
 *          MapUnit unwrappedUnit = mapUnitThing.GetValue<MapUnit>();
 *          //Do whatever you like with unwrappedUnit...
 *      }
 **/
public class ParameterWrapper : Godot.Object
{
    private readonly object value;

    public ParameterWrapper(object value)
    {
        this.value = value;
    }

    public T GetValue<T>()
    {
        return (T)this.value;
    }
}