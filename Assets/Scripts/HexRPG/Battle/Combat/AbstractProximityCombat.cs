
namespace HexRPG.Battle.Combat
{
    //! �ߐ�(�R���{����)Combat
    public abstract class AbstractProximityCombat : AbstractCombatBehaviour
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
