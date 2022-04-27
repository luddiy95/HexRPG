
namespace HexRPG.Battle
{
    public class ActionEventDie : ActionEvent<ActionEventDie>
    {
        protected override ActionEventDie Self => this;

        public ActionEventDie() : base(0f)
        {
        }
    }
}