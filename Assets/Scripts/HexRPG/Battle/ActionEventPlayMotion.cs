
namespace HexRPG.Battle
{
    public class ActionEventPlayMotion : ActionEvent<ActionEventPlayMotion>
    {
        protected override ActionEventPlayMotion Self => this;

        //TODO: ‚ä‚­‚ä‚­‚ÍanimationName‚ð“n‚·
        public ActionEventPlayMotion(float start) : base(start)
        {
        }
    }
}
