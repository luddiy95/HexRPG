using Zenject;
using System;
using UnityEngine;
using UnityEngine.Playables;

namespace HexRPG.Battle.Enemy
{
    using Combat;
    using Skill;

    public class EnemyInstaller : MonoInstaller, ICombatEquipment, ISkillsEquipment
    {
        [Inject] Transform _spawnRoot;
        [Inject] Vector3 _spawnPos;

        GameObject ICombatEquipment.EquipmentPrefab => _equipmentPrefab;
        CombatType ICombatEquipment.CombatType => _combatType;
        GameObject ICombatEquipment.CombatPrefab => _combatPrefab;
        Transform ICombatEquipment.SpawnRoot => _combatSpawnRoot;
        PlayableAsset ICombatEquipment.Timeline => _combatTimeline;
        [Header("通常攻撃")]
        [SerializeField] GameObject _equipmentPrefab;
        [SerializeField] CombatType _combatType;
        [SerializeField] GameObject _combatPrefab;
        [SerializeField] Transform _combatSpawnRoot;
        [SerializeField] PlayableAsset _combatTimeline;

        SkillAsset[] ISkillsEquipment.Skills => _skills;
        [Header("スキルリスト")]
        [SerializeField] SkillAsset[] _skills;

        public override void InstallBindings()
        {
            Container.BindInterfacesAndSelfTo<EnemyOwner>().FromComponentOnRoot();
            Container.BindInstance(_spawnRoot).WhenInjectedInto<TransformBehaviour>();
            Container.BindInstance(_spawnPos).WhenInjectedInto<TransformBehaviour>();

            Container.BindInterfacesTo<ActionStateController>().AsSingle();

            Container.BindInterfacesTo<EnemyMover>().AsSingle();

            Container.BindInterfacesTo<EnemyCombatExecuter>().AsSingle();
            Container.BindInterfacesTo<EnemySkillExecuter>().AsSingle();

            Container.BindInterfacesTo<AttackController>().AsSingle();

            Container.BindInterfacesTo<DamagedApplicable>().AsSingle();

            Container.BindInterfacesTo<Health>().AsSingle();

            if(_combatPrefab != null)
            {
                Container.BindFactory<Transform, Vector3, CombatOwner, CombatOwner.Factory>()
                    .FromSubContainerResolve()
                    .ByNewContextPrefab<CombatInstaller>(_combatPrefab);
            }

            Array.ForEach(_skills, skill =>
            {
                Container.BindFactory<Transform, Vector3, SkillOwner, SkillOwner.Factory>()
                    .FromSubContainerResolve()
                    .ByNewContextPrefab<SkillInstaller>(skill.Prefab);
            });
        }
    }
}
