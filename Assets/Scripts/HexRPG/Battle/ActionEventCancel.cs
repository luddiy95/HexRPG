
namespace HexRPG.Battle
{
    public class ActionEventCancel : ActionEvent<ActionEventCancel>
    {
        protected override ActionEventCancel Self => this;

        public string CommandId { get; private set; }
        public ActionStateType StateType { get; private set; }

        public bool HasStateType => StateType != ActionStateType.NONE;

        // Stateを脱出する時、そのStateに登録された全てのActionEventへのEnd通知を発行しない
        public bool PassEndNotification { get; private set; }

        public ActionEventCancel(string commandId, float start, float end, ActionStateType stateType, bool passEndNotification = false) : base(start, end)
        {
            CommandId = commandId;
            StateType = stateType;
            PassEndNotification = passEndNotification;
        }

        public ActionEventCancel(string commandId, float start, ActionStateType stateType, bool passEndNotification = false) : base(start)
        {
            CommandId = commandId;
            StateType = stateType;
            PassEndNotification = passEndNotification;
        }

        public ActionEventCancel(string commandId, ActionStateType stateType, bool passEndNotification = false) : base(0f)
        {
            CommandId = commandId;
            StateType = stateType;
            PassEndNotification = passEndNotification;
        }

        public ActionEventCancel(string commandId, float start, bool passEndNotification = false) : base(start)
        {
            CommandId = commandId;
            StateType = ActionStateType.NONE;
            PassEndNotification = passEndNotification;
        }
    }
}
