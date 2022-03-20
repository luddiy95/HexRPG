using UnityEngine;
using System;
using Zenject;
using UnityEngine.Playables;

namespace HexRPG.Battle.Player.Member
{
    using Combat;
    using Skill;

    public class MemberInstaller : MonoInstaller, ICombatSetting, ISkillsSetting
    {
        GameObject ICombatSetting.Prefab => _combatPrefab;
        Transform ICombatSetting.SpawnRoot => _combatSpawnRoot;
        PlayableAsset ICombatSetting.Timeline => _combatTimeline;
        [Header("通常攻撃")]
        [SerializeField] GameObject _combatPrefab;
        [SerializeField] Transform _combatSpawnRoot;
        [SerializeField] PlayableAsset _combatTimeline;

        SkillAsset[] ISkillsSetting.Skills => _skills;
        [Header("スキルリスト")]
        [SerializeField] SkillAsset[] _skills;

        [Inject] Transform _spawnRoot;
        [Inject] Vector3 _spawnPos;

        public override void InstallBindings()
        {
            Container.BindInterfacesAndSelfTo<MemberOwner>().FromComponentOnRoot();
            Container.BindInstance(_spawnRoot).WhenInjectedInto<TransformBehaviour>();
            Container.BindInstance(_spawnPos).WhenInjectedInto<TransformBehaviour>();

            Container.BindInterfacesTo<MemberCombatExecuter>().AsSingle();
            Container.BindInterfacesTo<MemberSkillExecuter>().AsSingle();

            Container.BindInterfacesTo<Mental>().AsSingle();
            Container.BindInterfacesTo<Health>().AsSingle();

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
