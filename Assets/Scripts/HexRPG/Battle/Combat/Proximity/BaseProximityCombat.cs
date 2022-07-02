
namespace HexRPG.Battle.Combat
{
    //! �ߐ�(�R���{����)Combat
    public class BaseProximityCombat : AbstractCombatBehaviour
    {
        // �U��������Ȃ���
        protected override void OnAttackDisable()
        {
            FinishAttack();
            base.OnAttackDisable();
        }

        // �A�j���[�V�������for����I�����A�U��������Ȃ���
        protected override void OnFinishCombat()
        {
            OnAttackDisable();
            base.OnFinishCombat();
        }
    }
}
