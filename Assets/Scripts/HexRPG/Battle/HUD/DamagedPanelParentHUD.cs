using UnityEngine;
using Zenject;
using UniRx;

namespace HexRPG.Battle.HUD
{
    public class DamagedPanelParentHUD : MonoBehaviour, ICharacterHUD
    {
        void ICharacterHUD.Bind(ICharacterComponentCollection character)
        {
            GetComponent<RectTransform>().anchoredPosition = Vector2.zero;

            character.DieObservable.OnFinishDie // IsDead時にDestroyすると死ぬ直前のダメージもすぐ消えてしまう
                .Subscribe(_ => Destroy(gameObject))
                .AddTo(this);

            for (int i = 0; i < transform.childCount; i++)
            {
                var huds = transform.GetChild(i).GetComponents<ICharacterHUD>();
                foreach (var hud in huds) hud.Bind(character);
            }
        }

        public class Factory : PlaceholderFactory<DamagedPanelParentHUD>
        {

        }
    }
}
