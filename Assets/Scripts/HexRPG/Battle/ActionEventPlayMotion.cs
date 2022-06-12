
namespace HexRPG.Battle
{
    public class ActionEventPlayMotion : ActionEvent<ActionEventPlayMotion>
    {
        protected override ActionEventPlayMotion Self => this;

        public ActionEventPlayMotion(float start) : base(start)
        {
        }
    }
}
