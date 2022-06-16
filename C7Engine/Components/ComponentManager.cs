using System;
using System.Collections.Generic;
using System.Linq;

// modeled after the Unity Toolbox pattern from https://wiki.unity3d.com/index.php/Toolbox
namespace C7Engine.Components
{
    public sealed class ComponentManager
    {
        // singleton implemenation taken from example at https://csharpindepth.com/Articles/Singleton
        private static readonly ComponentManager _instance = new ComponentManager();

        static ComponentManager()
        {

        }

        private ComponentManager()
        {

        }

        public static ComponentManager Instance
        {
            get { return _instance; }
        }

        // type dictionary taken from Jon Skeet's implementation at https://codeblog.jonskeet.uk/2008/10/08/mapping-from-a-type-to-an-instance-of-that-type/
        private Dictionary<Type, GameComponent> _components = new Dictionary<Type, GameComponent>();

        public ComponentManager AddComponent<T>(T component) where T : GameComponent
        {
            _components.Add(typeof(T), component);
            return this;
        }

        public T GetComponent<T>() where T : GameComponent
        {
            GameComponent component;
            if (_components.TryGetValue(typeof(T), out component))
            {
                return (T)component;
            }
            return default(T);
        }

        public void InitializeComponents()
        {
            _components.ToList().ForEach(c => c.Value.Initialize());
        }
    }

    public interface GameComponent
    {
        public void Initialize();
    }
}