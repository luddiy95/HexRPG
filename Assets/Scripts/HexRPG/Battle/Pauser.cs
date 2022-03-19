using System;
using UniRx;

namespace HexRPG.Battle
{
    public interface IPauseController
    {
        void StartPause();
        void Restart();
    }

    public interface IPauseObservable
    {
        IObservable<Unit> OnPause { get; }
        IObservable<Unit> OnRestart { get; }
    }

    public class Pauser : IPauseController, IPauseObservable
    {
        IObservable<Unit> IPauseObservable.OnPause => _onPause;
        readonly ISubject<Unit> _onPause = new Subject<Unit>();

        IObservable<Unit> IPauseObservable.OnRestart => _onRestart;
        readonly ISubject<Unit> _onRestart = new Subject<Unit>();

        void IPauseController.StartPause()
        {
            _onPause.OnNext(Unit.Default);
        }

        void IPauseController.Restart()
        {
            _onRestart.OnNext(Unit.Default);
        }
    }
}
