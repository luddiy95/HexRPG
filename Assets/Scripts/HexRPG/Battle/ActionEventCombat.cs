
namespace HexRPG.Battle
{
    public class ActionEventCombat : ActionEvent<ActionEventCombat>
    {
        protected override ActionEventCombat Self => this;

        public ActionEventCombat() : base(0f)
        {
        }
    }
}
