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
            Container.BindInstance(_spawnRoot).WhenInjectedInto<TransformBehaviour>();
            Container.BindInstance(_spawnPos).WhenInjectedInto<TransformBehaviour>();

            Container.BindInterfacesTo<MemberController>().AsSingle();

            Container.BindInterfacesTo<SkillSelecter>().AsSingle();
            Container.BindInterfacesTo<PlayerSkillExecuter>().AsSingle();

            Container.BindInterfacesTo<PlayerActionStateController>().AsSingle();
            Container.BindInterfacesTo<ActionStateController>().AsSingle();
            Container.BindInterfacesTo<Pauser>().AsSingle();
            Container.BindInterfacesTo<PlayerMover>().AsSingle();

            System.Array.ForEach(_party, memberPrefab =>
            {
                Container.BindFactory<Transform, Vector3, MemberOwner, MemberOwner.Factory>()
                    .FromSubContainerResolve()
                    .ByNewContextPrefab<MemberInstaller>(memberPrefab);
            });
        }
    }
}
