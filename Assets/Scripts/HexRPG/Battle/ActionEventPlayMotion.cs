
namespace HexRPG.Battle
{
    public class ActionEventPlayMotion : ActionEvent<ActionEventPlayMotion>
    {
        protected override ActionEventPlayMotion Self => this;

        //TODO: �䂭�䂭��animationName��n��
        public ActionEventPlayMotion(float start) : base(start)
        {
        }
    }
}
