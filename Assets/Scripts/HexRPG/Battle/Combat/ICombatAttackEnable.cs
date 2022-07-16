using UnityEngine;

namespace HexRPG.Battle.Combat
{
    public interface ICombatAttack
    {
        void OnAttackEnable(int damage, Vector3 colliderVelocity);
        void OnAttackDisable();
    }
}
