using UnityEngine;
using System;
using Zenject;

namespace HexRPG.Battle.Player.Member
{
    using Skill;

    public class MemberInstaller : MonoInstaller, ISkillListSetting
    {
        GameObject[] ISkillListSetting.SkillList => _skillList;
        [Header("スキルリスト")]
        [SerializeField] GameObject[] _skillList;

        [Inject] Transform _spawnRoot;
        [Inject] Vector3 _spawnPos;

        public override void InstallBindings()
        {
            Container.BindInterfacesAndSelfTo<MemberOwner>().FromComponentOnRoot();
            Container.BindInstance(_spawnRoot).WhenInjectedInto<TransformBehaviour>();
            Container.BindInstance(_spawnPos).WhenInjectedInto<TransformBehaviour>();

            Container.BindInterfacesTo<Mental>().AsSingle();
            Container.BindInterfacesTo<Health>().AsSingle();

            Array.ForEach(_skillList, skill =>
            {
                Container.BindFactory<Transform, Vector3, SkillOwner, SkillOwner.Factory>()
                    .FromSubContainerResolve()
                    .ByNewContextPrefab<SkillInstaller>(skill);
            });
        }
    }
}
