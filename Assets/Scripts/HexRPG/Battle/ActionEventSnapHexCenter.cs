
namespace HexRPG.Battle
{
    public class ActionEventSnapHexCenter : ActionEvent<ActionEventSnapHexCenter>
    {
        protected override ActionEventSnapHexCenter Self => this;

        public ActionEventSnapHexCenter(float start) : base(start)
        {
        }
    }
}