using System.Collections.Generic;
using UnityEngine;

namespace HexRPG.Battle.Enemy.HUD
{
    public class EnemyStatusHUD : MonoBehaviour, ICharacterHUD
    {
        [SerializeField] GameObject _healthGaugePrefab;
        [SerializeField] Transform _healthGaugeRoot;

        void ICharacterHUD.Bind(ICharacterComponentCollection chara)
        {
            //TODO:
            /*
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
            */
        }
    }
}
