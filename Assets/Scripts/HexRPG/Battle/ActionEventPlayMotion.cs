
namespace HexRPG.Battle
{
    public class ActionEventPlayMotion : ActionEvent<ActionEventPlayMotion>
    {
        protected override ActionEventPlayMotion Self => this;

        //TODO: ゆくゆくはanimationNameを渡す
        public ActionEventPlayMotion(float start) : base(start)
        {
        }
    }
}
