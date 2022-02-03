
namespace C7Engine
{

using C7GameData;

public delegate bool OnAnimationCompleted(string unitGUID, MapUnit.AnimatedAction action);

public interface IAnimationControl
{
	void startAnimation(MapUnit unit, MapUnit.AnimatedAction action, OnAnimationCompleted callback);
	void endAnimation(MapUnit unit, bool triggerCallback = true);
}

}
