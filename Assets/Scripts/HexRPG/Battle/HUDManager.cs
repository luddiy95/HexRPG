using UnityEngine;
using System.Linq;
using UniRx;

namespace HexRPG.Battle
{
    public class HUDManager : AbstractCustomComponentBehaviour
    {
        [Header("CustomComponentBehaviour のHUD実装オブジェクト")]
        [SerializeField] GameObject[] _subHUDList = new GameObject[0];

        public override void Initialize()
        {
            base.Initialize();

            // Owner = 自分自身ComponentCollection
            if (!Owner.QueryInterface(out IComponentCollectionFactory factory)) return;

            var hudInstances = _subHUDList.Select(x => factory.CreateComponentCollectionWithoutInstantiate(x)).ToList();

            if (Owner.QueryInterface(out IBattleObservable battleObservable))
            {
                foreach (var hud in hudInstances)
                {
                    if (hud.QueryInterface(out ICharacterHUD characterHUD))
                    {
                        battleObservable.OnPlayerSpawn
                            .Subscribe(player => characterHUD.Bind(player))
                            .AddTo(this);

                        //TODO: EnemyのHUDはテンプレからクローンしないといけない
                    }
                }
            }
        }
    }
}
