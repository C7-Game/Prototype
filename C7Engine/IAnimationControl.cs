
namespace C7Engine
{

using C7GameData;

public delegate bool OnAnimationCompleted(string unitGUID, MapUnit.AnimatedAction action);

public interface IAnimationControl
{
	void startAnimation(string unitGUID, MapUnit.AnimatedAction action, OnAnimationCompleted callback);
	void endAnimation(string unitGUID, bool triggerCallback = true);
}

}
