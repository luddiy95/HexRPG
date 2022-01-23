
namespace HexRPG.Battle
{
    public abstract class ActionEvent<SELF> : AbstractActionEvent where SELF : AbstractActionEvent
    {
        protected abstract SELF Self { get; }

        public override float Start => _start;
        public override float End => _end;

        readonly float _start;
        readonly float _end;

        public ActionEvent(float start, float end)
        {
            _start = start;
            _end = end;
        }

        public ActionEvent(float start)
        {
            _start = start;
            _end = float.PositiveInfinity;
        }

        public override void OnStart(IActionEventNotifier notifier)
        {
            notifier.OnStart(Self);
        }

        public override void OnEnd(IActionEventNotifier notifier)
        {
            notifier.OnEnd(Self);
        }
    }
}
