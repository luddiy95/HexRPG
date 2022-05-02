
namespace HexRPG.Battle
{
    public class ActionEventRotate : ActionEvent<ActionEventRotate>
    {
        protected override ActionEventRotate Self => this;

        public ActionEventRotate() : base(0f)
        {
        }
    }
}
