
namespace HexRPG.Battle
{
    public class ActionEventIdle : ActionEvent<ActionEventIdle>
    {
        protected override ActionEventIdle Self => this;

        public ActionEventIdle(float start) : base(start)
        {
        }
    }
}