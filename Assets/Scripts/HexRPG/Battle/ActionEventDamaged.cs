
namespace HexRPG.Battle
{
    public class ActionEventDamaged : ActionEvent<ActionEventDamaged>
    {
        protected override ActionEventDamaged Self => this;

        public ActionEventDamaged() : base(0f)
        {
        }
    }
}