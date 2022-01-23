
namespace HexRPG.Battle
{
    public class ActionEventPause : ActionEvent<ActionEventPause>
    {
        protected override ActionEventPause Self => this;

        public ActionEventPause(float start) : base(start)
        {
        }
    }
}