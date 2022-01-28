using System.Collections.Generic;
using UnityEngine;

namespace HexRPG.Battle.Enemy.HUD
{
    public class EnemyStatusHUD : AbstractCustomComponentBehaviour, ICharacterHUD
    {
        [SerializeField] GameObject _healthGaugePrefab;

        [SerializeField] Transform _healthGaugeRoot;

        public override void Register(ICustomComponentCollection owner)
        {
            base.Register(owner);

            owner.RegisterInterface<ICharacterHUD>(this);
        }

        public override void Initialize()
        {
            base.Initialize();
        }

        void ICharacterHUD.Bind(ICustomComponentCollection chara)
        {
            if (!Owner.QueryInterface(out IComponentCollectionFactory factory)) return;

            var clone = Instantiate(_healthGaugePrefab);
            clone.transform.SetParent(_healthGaugeRoot);
            var hpGauge = factory.CreateComponentCollectionWithoutInstantiate(clone, null, null);
            if(hpGauge.QueryInterfaces(out IEnumerable<ICharacterHUD> characterHUDs))
            {
                foreach (var chud in characterHUDs)
                {
                    chud.Bind(chara);
                }
            }
        }
    }
}
