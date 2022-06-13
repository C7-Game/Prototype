using System;

namespace C7Engine
{
    public class TurnEventArgs : EventArgs
    {
        public GameTurn Turn { get; }

        public TurnEventArgs(GameTurn turn)
        {
            Turn = turn;
        }
    }

    public class GameTurn
    {
        public int TurnNumber { get; }
        public string TurnDate { get; }

        public GameTurn(int num, string date)
        {
            TurnNumber = num;
            TurnDate = date;
        }
    }

    public class CalendarComponent : GameComponent
    {
        public event EventHandler<TurnEventArgs> TurnStarted;
        public GameTurn CurrentTurn { get; private set; }
        //TODO add interace for turn settings

        public CalendarComponent()
        {
            CurrentTurn = GetGameTurn(EngineStorage.gameData.turn);
            TurnHandling.TurnEnded += (obj, args) => AdvanceTurn();
            Console.WriteLine("Initialized CalendarComponent at turn " + CurrentTurn.TurnNumber);
        }

        private void AdvanceTurn()
        {
            Console.WriteLine("Ending turn " + CurrentTurn.TurnNumber);
            int nextTurnNumber = CurrentTurn.TurnNumber + 1;
            CurrentTurn = GetGameTurn(nextTurnNumber);
            EngineStorage.gameData.turn = nextTurnNumber;
            Console.WriteLine("Date is now " + CurrentTurn.TurnDate);
            TurnStarted?.Invoke(this, new TurnEventArgs(CurrentTurn));
        }

        public GameTurn GetGameTurn(int turnNumber)
        {
            // TODO use some interface to calculate the date
            // based on turn settings in the rules 
            int year = -4000 + (50 * (turnNumber-1));
            string era = year < 0 ? "BC" : "AD";
            return new GameTurn(turnNumber, $"{Math.Abs(year)} {era}");
        }
    }
}