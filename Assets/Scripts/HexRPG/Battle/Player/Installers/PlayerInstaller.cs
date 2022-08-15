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

        [Inject] Transform _spawnRoot;
        [Inject] Vector3 _spawnPos;

        public override void InstallBindings()
        {
            //! FromSubContainerResolveバインディングで親コンテナにバインドされるクラスはサブコンテナ(ここ)内でバインドされなければならない
            Container.BindInterfacesAndSelfTo<PlayerOwner>().FromComponentOnRoot();

            Container.BindInterfacesTo<PlayerCameraController>().AsSingle();

            Container.BindInterfacesTo<MemberController>().AsSingle();

            Container.BindInterfacesTo<PlayerCombatExecuter>().AsSingle();

            Container.BindInterfacesTo<SkillSelecter>().AsSingle();
            Container.BindInterfacesTo<PlayerSkillExecuter>().AsSingle();

            Container.BindInterfacesTo<PlayerAttackController>().AsSingle();
            Container.BindInterfacesTo<Liberater>().AsSingle();

            Container.BindInterfacesTo<PlayerDamagedApplicable>().AsSingle();

            Container.BindInterfacesTo<ActionStateController>().AsSingle();

            Container.BindInterfacesTo<PlayerDieObservable>().AsSingle();

            System.Array.ForEach(_party, memberPrefab =>
            {
                Container.BindFactory<Transform, Vector3, MemberOwner, MemberOwner.Factory>()
                    .FromSubContainerResolve()
                    .ByNewContextPrefab<MemberInstaller>(memberPrefab);
            });
        }
    }
}
