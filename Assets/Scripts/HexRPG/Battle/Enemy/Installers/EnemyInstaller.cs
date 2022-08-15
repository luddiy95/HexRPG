using System.Collections.Generic;
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
        GameObject ICombatEquipment.EquipmentPrefab => _equipmentPrefab;
        Transform ICombatEquipment.EquipmentRoot => _equipmentRoot;
        CombatType ICombatEquipment.CombatType => _combatType;
        GameObject ICombatEquipment.CombatPrefab => _combatPrefab;
        PlayableAsset ICombatEquipment.Timeline => _combatTimeline;
        [Header("通常攻撃")]
        [SerializeField] GameObject _equipmentPrefab;
        [SerializeField] CombatType _combatType;
        [SerializeField] GameObject _combatPrefab;
        [SerializeField] Transform _equipmentRoot;
        [SerializeField] PlayableAsset _combatTimeline;

        IReadOnlyList<SkillAsset> ISkillsEquipment.Skills => _skills;
        [Header("スキルリスト")]
        [SerializeField] SkillAsset[] _skills;

        public override void InstallBindings()
        {
            Container.BindInterfacesAndSelfTo<EnemyOwner>().FromComponentOnRoot();

            Container.BindInterfacesTo<ActionStateController>().AsSingle();

            Container.BindInterfacesTo<EnemyCombatExecuter>().AsSingle();
            Container.BindInterfacesTo<EnemySkillExecuter>().AsSingle();

            Container.BindInterfacesTo<EnemyAttackController>().AsSingle();
            Container.BindInterfacesTo<Liberater>().AsSingle();

            Container.BindInterfacesTo<EnemyDamagedApplicable>().AsSingle();

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
