using System.Collections.Generic;

namespace HexRPG.Battle
{
    public interface ICombatAttackSetting : IAttackSetting
    {
        List<AttackCollider> AttackColliders { get; }
    }

    public class CombatAttackSetting : ICombatAttackSetting
    {
        int IAttackSetting.Power => power;
        public int power;

        List<AttackCollider> ICombatAttackSetting.AttackColliders => attackColliders;
        public List<AttackCollider> attackColliders;
    }
}
