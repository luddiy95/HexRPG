using System.Collections.Generic;
using UnityEngine;
using System;
using Zenject;
using UnityEngine.Playables;

namespace HexRPG.Battle.Player.Member
{
    using Combat;
    using Skill;

    public class MemberInstaller : MonoInstaller, ICombatEquipment, ISkillsEquipment
    {
        GameObject ICombatEquipment.EquipmentPrefab => _equipmentPrefab;
        Transform ICombatEquipment.EquipmentRoot => _equipmentRoot;
        CombatType ICombatEquipment.CombatType => _combatType;
        GameObject ICombatEquipment.CombatPrefab => _combatPrefab;
        PlayableAsset ICombatEquipment.Timeline => _combatTimeline;
        [Header("通常攻撃")]
        [SerializeField] GameObject _equipmentPrefab;
        [SerializeField] Transform _equipmentRoot;
        [SerializeField] CombatType _combatType;
        [SerializeField] GameObject _combatPrefab;
        [SerializeField] PlayableAsset _combatTimeline;

        IReadOnlyList<SkillAsset> ISkillsEquipment.Skills => _skills;
        [Header("スキルリスト")]
        [SerializeField] SkillAsset[] _skills;

        [Inject] Transform _spawnRoot;
        [Inject] Vector3 _spawnPos;

        public override void InstallBindings()
        {
            Container.BindInterfacesAndSelfTo<MemberOwner>().FromComponentOnRoot();

            Container.BindInterfacesTo<MemberCombatExecuter>().AsSingle();
            Container.BindInterfacesTo<MemberSkillExecuter>().AsSingle();

            Container.BindInterfacesTo<Health>().AsSingle();
            Container.BindInterfacesTo<SkillPoint>().AsSingle();

            Container.BindFactory<Transform, Vector3, CombatOwner, CombatOwner.Factory>()
                .FromSubContainerResolve()
                .ByNewContextPrefab<CombatInstaller>(_combatPrefab);

            Array.ForEach(_skills, skill =>
            {
                Container.BindFactory<Transform, Vector3, SkillOwner, SkillOwner.Factory>()
                    .FromSubContainerResolve()
                    .ByNewContextPrefab<SkillInstaller>(skill.Prefab);
            });
        }
    }
}
