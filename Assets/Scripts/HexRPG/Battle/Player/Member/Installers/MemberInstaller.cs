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

        public override void InstallBindings()
        {
            Container.BindInterfacesTo<Mental>().AsSingle();
            Container.BindInterfacesTo<Health>().AsSingle();

            Container.BindInterfacesTo<Transform>().FromInstance(transform);

            Array.ForEach(_skillList, skill =>
            {
                Container.BindFactory<SkillOwner, SkillOwner.Factory>().FromComponentInNewPrefab(skill);
            });
        }
    }
}
