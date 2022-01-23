
namespace HexRPG.Battle
{
    public class ActionEventCancel : ActionEvent<ActionEventCancel>
    {
        protected override ActionEventCancel Self => this;

        public string CommandId { get; private set; }
        public ActionStateType StateType { get; private set; }

        public bool HasStateType => StateType != ActionStateType.NONE;

        public ActionEventCancel(string commandId, float start, float end, ActionStateType stateType) : base(start, end)
        {
            CommandId = commandId;
            StateType = stateType;
        }

        public ActionEventCancel(string commandId, float start, ActionStateType stateType) : base(start)
        {
            CommandId = commandId;
            StateType = stateType;
        }

        public ActionEventCancel(string commandId, float start) : base(start)
        {
            CommandId = commandId;
            StateType = ActionStateType.NONE;
        }
    }
}
