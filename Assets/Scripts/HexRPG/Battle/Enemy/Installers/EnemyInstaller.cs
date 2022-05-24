using Zenject;
using System;
using UnityEngine;

namespace HexRPG.Battle.Enemy
{
    using Skill;

    public class EnemyInstaller : MonoInstaller, ISkillsSetting
    {
        [Inject] Transform _spawnRoot;
        [Inject] Vector3 _spawnPos;

        SkillAsset[] ISkillsSetting.Skills => _skills;
        [Header("スキルリスト")]
        [SerializeField] SkillAsset[] _skills;

        public override void InstallBindings()
        {
            Container.BindInterfacesAndSelfTo<EnemyOwner>().FromComponentOnRoot();
            Container.BindInstance(_spawnRoot).WhenInjectedInto<TransformBehaviour>();
            Container.BindInstance(_spawnPos).WhenInjectedInto<TransformBehaviour>();

            Container.BindInterfacesTo<ActionStateController>().AsSingle();

            Container.BindInterfacesTo<EnemyMover>().AsSingle();

            Container.BindInterfacesTo<EnemySkillExecuter>().AsSingle();

            Container.Bind(typeof(IAttackReserve), typeof(IAttackController)).To<AttackController>().AsSingle();

            Container.BindInterfacesTo<Health>().AsSingle();

            Array.ForEach(_skills, skill =>
            {
                Container.BindFactory<Transform, Vector3, SkillOwner, SkillOwner.Factory>()
                    .FromSubContainerResolve()
                    .ByNewContextPrefab<SkillInstaller>(skill.Prefab);
            });
        }
    }
}
