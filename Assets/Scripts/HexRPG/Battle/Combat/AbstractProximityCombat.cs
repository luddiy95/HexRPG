
namespace HexRPG.Battle.Combat
{
    //! 近接(コンボ込み)Combat
    public abstract class AbstractProximityCombat : AbstractCombatBehaviour
    {
        // 攻撃判定をなくす
        protected override void OnAttackDisable()
        {
            FinishAttack();
            base.OnAttackDisable();
        }

        // アニメーション中断or正常終了時、攻撃判定をなくす
        protected override void OnFinishCombat()
        {
            OnAttackDisable();
            base.OnFinishCombat();
        }
    }
}
