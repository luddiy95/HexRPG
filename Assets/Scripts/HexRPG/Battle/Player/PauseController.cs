using System;
using UniRx;

namespace HexRPG.Battle.Player
{
    public interface IPauseController : IFeature
    {
        void StartPause();
        void Restart();
    }

    public interface IPauseObservable : IFeature
    {
        IObservable<Unit> OnPause { get; }
        IObservable<Unit> OnRestart { get; }
    }

    public class PauseController : AbstractCustomComponentBehaviour, IPauseController, IPauseObservable
    {
        IObservable<Unit> IPauseObservable.OnPause => _onPause;
        ISubject<Unit> _onPause = new Subject<Unit>();

        IObservable<Unit> IPauseObservable.OnRestart => _onRestart;
        ISubject<Unit> _onRestart = new Subject<Unit>();

        public override void Register(ICustomComponentCollection owner)
        {
            base.Register(owner);

            owner.RegisterInterface<IPauseController>(this);
            owner.RegisterInterface<IPauseObservable>(this);
        }

        public override void Initialize()
        {
            base.Initialize();
        }

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
