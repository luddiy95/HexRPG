using UnityEngine;
using UnityEditor;
using System;
using Zenject;

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

        //TODO: Decorator—p
        IActionStateObservable ActionStateObservable { get; }
    }

    public class EnemyOwner : AbstractPoolableOwner<EnemyOwner>, IEnemyComponentCollection
    {
        [Inject] IProfileSetting ICharacterComponentCollection.ProfileSetting { get; }
        [Inject] IDieObservable ICharacterComponentCollection.DieObservable { get; }
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

        //TODO: Decorator—p
        [Inject] IActionStateObservable IEnemyComponentCollection.ActionStateObservable { get; }

#if UNITY_EDITOR

        IEnemyComponentCollection _enemyOwner => this;

        public void OnInspectorGUI()
        {
            if (GUILayout.Button("Damage"))
            {
                _enemyOwner.DamageApplicable.OnHitTest(10);
            }
            if (GUILayout.Button("Die"))
            {
                _enemyOwner.DamageApplicable.OnHitTest(_enemyOwner.Health.Max);
            }
        }

        [CustomEditor(typeof(EnemyOwner))]
        public class CustomInspector : Editor
        {
            public override void OnInspectorGUI()
            {
                base.OnInspectorGUI();

                ((EnemyOwner)target).OnInspectorGUI();
            }
        }

#endif
    }
}
