using Zenject;
using UnityEngine;

namespace HexRPG.Battle.Player
{
    using Member;

    public class PlayerInstaller : MonoInstaller, IPartySetting
    {
        GameObject[] IPartySetting.Party => _party;

        [Header("パーティのメンバーリスト")]
        [SerializeField] GameObject[] _party;

        public override void InstallBindings()
        {
            Container.BindInterfacesTo<MemberController>().AsSingle();

            Container.BindInterfacesTo<SkillSelecter>().AsSingle();
            Container.BindInterfacesTo<PlayerSkillExecuter>().AsSingle();

            Container.BindInterfacesTo<PlayerActionStateController>().AsSingle();
            Container.BindInterfacesTo<ActionStateController>().AsSingle();
            Container.BindInterfacesTo<Pauser>().AsSingle();
            Container.BindInterfacesTo<PlayerMover>().AsSingle();

            System.Array.ForEach(_party, memberPrefab =>
            {
                Container.BindFactory<MemberOwner, MemberOwner.Factory>().FromComponentInNewPrefab(memberPrefab);
            });
        }
    }
}
