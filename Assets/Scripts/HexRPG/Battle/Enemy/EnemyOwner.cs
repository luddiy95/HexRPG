using UnityEngine;
using Zenject;
using UniRx;

namespace HexRPG.Battle.Enemy
{
    public interface IEnemyComponentCollection : IAttackComponentCollection
    {
        INavMeshAgentController NavMeshAgentController { get; }
        IActiveController ActiveController { get; }
        IDieController DieController { get; }
        IAnimationController AnimationController { get; }
        ICombatSpawnObservable CombatSpawnObservable { get; }
        ICombatController CombatController { get; }
        ICharacterActionStateController CharacterActionStateController { get; }
        ISkillSpawnObservable SkillSpawnObservable { get; }

        //TODO: Decoratoróp
        IActionStateObservable ActionStateObservable { get; }
    }

    public class EnemyOwner : MonoBehaviour, IEnemyComponentCollection
    {
        [Inject] IProfileSetting ICharacterComponentCollection.ProfileSetting { get; }
        [Inject] IDieObservable ICharacterComponentCollection.DieObservable { get; }
        [Inject] ITransformController ICharacterComponentCollection.TransformController { get; }
        [Inject] IHealth ICharacterComponentCollection.Health { get; }

        [Inject] IAttackApplicator IAttackComponentCollection.AttackApplicator { get; }
        [Inject] IAttackController IAttackComponentCollection.AttackController { get; }
        [Inject] IAttackObservable IAttackComponentCollection.AttackObservable { get; }
        [Inject] IDamageApplicable IAttackComponentCollection.DamageApplicable { get; }
        [Inject] ILiberateObservable IAttackComponentCollection.LiberateObservable { get; }

        [Inject] INavMeshAgentController IEnemyComponentCollection.NavMeshAgentController { get; }
        [Inject] IActiveController IEnemyComponentCollection.ActiveController { get; }
        [Inject] IDieController IEnemyComponentCollection.DieController { get; }
        [Inject] IAnimationController IEnemyComponentCollection.AnimationController { get; }
        [Inject] ICombatSpawnObservable IEnemyComponentCollection.CombatSpawnObservable { get; }
        [Inject] ICombatController IEnemyComponentCollection.CombatController { get; }
        [Inject] ICharacterActionStateController IEnemyComponentCollection.CharacterActionStateController { get; }
        [Inject] ISkillSpawnObservable IEnemyComponentCollection.SkillSpawnObservable { get; }

        //TODO: Decoratoróp
        [Inject] IActionStateObservable IEnemyComponentCollection.ActionStateObservable { get; }

        void OnDestroy()
        {
            //! runtimeèIóπéû
            (this as IEnemyComponentCollection).DieController.ForceDie();
        }

        public class Factory : PlaceholderFactory<Transform, Vector3, EnemyOwner>
        {

        }
    }
}
