using UnityEngine;
using Zenject;
using System;

namespace HexRPG.Battle.Enemy.HUD
{
    public class EnemyStatusHUD : MonoBehaviour, ICharacterHUD
    {
        EnemyHealthGauge.Factory _factory;
        [SerializeField] Transform _healthGaugeRoot;

        [Inject]
        public void Construct(EnemyHealthGauge.Factory factory)
        {
            _factory = factory;
        }

        void ICharacterHUD.Bind(ICharacterComponentCollection chara)
        {
            var clone = _factory.Create();
            clone.transform.SetParent(_healthGaugeRoot);
            var huds = clone.GetComponents<ICharacterHUD>();
            Array.ForEach(huds, hud => hud.Bind(chara));
        }
    }
}
