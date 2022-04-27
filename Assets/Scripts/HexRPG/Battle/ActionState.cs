using System.Collections.Generic;

namespace HexRPG.Battle
{
    public enum ActionStateType
    {
        NONE,

        IDLE,

        MOVE,

        DAMAGED,

        COMBAT,

        SKILL_SELECT,
        SKILL,

        DIE,

        PAUSE
    }

    public class ActionState
    {
        public ActionStateType Type => _type;
        public IList<AbstractActionEvent> Events => _events;

        readonly ActionStateType _type;
        readonly List<AbstractActionEvent> _events = new List<AbstractActionEvent>();

        public ActionState(ActionStateType type)
        {
            _type = type;
        }

        public ActionState AddEvent<T>(T actionEvent) where T : ActionEvent<T>
        {
            _events.Add(actionEvent);
            return this;
        }
    }
}
