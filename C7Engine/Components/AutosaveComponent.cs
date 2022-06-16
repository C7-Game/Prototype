using System;
using System.IO;
using System.Linq;
using C7GameData;

namespace C7Engine.Components
{
    public class GameSaveEventArgs : EventArgs
    {
        public string SaveFilePath { get; }

        public GameSaveEventArgs(string path)
        {
            SaveFilePath = path;
        }
    }

    public class AutosaveComponent : GameComponent
    {
        public event EventHandler AutosaveCreated;
        private GameData _gameData;
        
        public AutosaveComponent(GameData gameData)
        {
            _gameData = gameData;
        }

        public void Initialize()
        {
            ComponentManager.Instance.GetComponent<CalendarComponent>().TurnStarted += OnTurnStarted;
        }

        public void OnTurnStarted(object source, TurnEventArgs args)
        {
            string date = args.Turn.TurnDate;
            date = String.Concat(date.Where(c => !char.IsWhiteSpace(c)));
            date = String.Join("_", date.Split(Path.GetInvalidFileNameChars(), StringSplitOptions.RemoveEmptyEntries));
            string filename = $"auto_{date}.json";
            Console.WriteLine($"I would save {filename} right now if I knew how...");
            AutosaveCreated?.Invoke(this, new GameSaveEventArgs(filename));
        }
    }
}