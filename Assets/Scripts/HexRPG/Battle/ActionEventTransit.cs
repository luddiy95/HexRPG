
namespace HexRPG.Battle
{
    public class ActionEventTransit : ActionEvent<ActionEventTransit>
    {
        protected override ActionEventTransit Self => this;

        public ActionStateType StateType { get; private set; }

        public ActionEventTransit(float start, ActionStateType stateType) : base(start)
        {
            StateType = stateType;
        }
    }
}
