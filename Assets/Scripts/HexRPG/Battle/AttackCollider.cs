using UnityEngine;

namespace HexRPG.Battle
{
    public class AttackCollider : MonoBehaviour
    {
        public IAttackApplicator AttackApplicator { get; set; }
    }
}
