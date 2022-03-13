using UnityEngine;
using Zenject;
using System;

namespace HexRPG.Battle.Enemy.HUD
{
    using Battle.HUD;

    public class EnemyStatusHUD : MonoBehaviour, ICharacterHUD
    {
        HealthGauge.Factory _factory;
        [SerializeField] Transform _healthGaugeRoot;

        [Inject]
        public void Construct(HealthGauge.Factory factory)
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
