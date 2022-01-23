
namespace HexRPG.Battle
{
    public class ActionEventMove : ActionEvent<ActionEventMove>
    {
        protected override ActionEventMove Self => this;

        public ActionEventMove(float start) : base(start)
        {
        }
    }
}
