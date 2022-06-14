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
        [Inject] Transform _spawnRoot;
        [Inject] Vector3 _spawnPos;

        GameObject ICombatEquipment.Prefab => _combatPrefab;
        Transform ICombatEquipment.SpawnRoot => _combatSpawnRoot;
        PlayableAsset ICombatEquipment.Timeline => _combatTimeline;
        [Header("通常攻撃")]
        [SerializeField] GameObject _combatPrefab;
        [SerializeField] Transform _combatSpawnRoot;
        [SerializeField] PlayableAsset _combatTimeline;

        SkillAsset[] ISkillsEquipment.Skills => _skills;
        [Header("スキルリスト")]
        [SerializeField] SkillAsset[] _skills;

        public override void InstallBindings()
        {
            Container.BindInterfacesAndSelfTo<MemberOwner>().FromComponentOnRoot();
            Container.BindInstance(_spawnRoot).WhenInjectedInto<TransformBehaviour>();
            Container.BindInstance(_spawnPos).WhenInjectedInto<TransformBehaviour>();

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
