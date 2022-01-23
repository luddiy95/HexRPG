
namespace HexRPG.Battle
{
    public class ActionEventSkill : ActionEvent<ActionEventSkill>
    {
        protected override ActionEventSkill Self => this;

        public ActionEventSkill(float start) : base(start)
        {
        }
    }
}
