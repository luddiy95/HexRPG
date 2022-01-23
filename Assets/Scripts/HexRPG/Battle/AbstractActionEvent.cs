
namespace HexRPG.Battle
{
    public abstract class AbstractActionEvent
    {
        public abstract float Start { get; }

        public abstract float End { get; }

        public abstract void OnStart(IActionEventNotifier notifier);

        public abstract void OnEnd(IActionEventNotifier notifier);
    }

    public interface IActionEventNotifier
    {
        void OnStart<T>(T stateEvent) where T : AbstractActionEvent;
        void OnEnd<T>(T stateEvent) where T : AbstractActionEvent;
    }
}
