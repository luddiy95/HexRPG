using UnityEngine;
using UnityEngine.UI;
using UniRx;
using UniRx.Triggers;

namespace HexRPG.Battle.Player.Panel
{
    using Character;

    public abstract class SelectBasePanelPresenter : MonoBehaviour
    {
        protected SelectBasePanelView _view;
        protected PlayerModel _playerModel;

        public virtual void Init(PlayerModel playerModel)
        {
            _view = GetComponent<SelectBasePanelView>();
            _playerModel = playerModel;

            SubscribeCharacterChange();
            SubscribeSelectPanelEvent();
            SubscribeSelectPanel();

            _view.CloseSelectPanel();
        }

        protected abstract void OnCharacterChanged(Character character);
        protected abstract void OnOptionBtnSelected(int index);

        void SubscribeCharacterChange()
        {
            _playerModel
                .CurCharacter
                .Subscribe(character =>
                {
                    OnCharacterChanged(character);
                })
                .AddTo(this);
        }

        protected virtual void SubscribeSelectPanelEvent()
        {
            void SubscribeOptionBtnEvent(Transform btn, int index)
            {
                ObservablePointerClickTrigger trigger;
                if (!btn.TryGetComponent(out trigger)) trigger = btn.gameObject.AddComponent<ObservablePointerClickTrigger>();
                trigger
                    .OnPointerClickAsObservable()
                    .Subscribe(_ =>
                    {
                        OnOptionBtnSelected(index);
                    });
            }

            ObservablePointerClickTrigger trigger;

            if (!_view.BtnDecide.TryGetComponent(out trigger)) trigger = _view.BtnDecide.AddComponent<ObservablePointerClickTrigger>();
            trigger
                .OnPointerClickAsObservable()
                .Subscribe(_ =>
                {
                    _playerModel.OnDecideBtnTouched();
                })
                .AddTo(this);

            if (!_view.BtnBack.TryGetComponent(out trigger)) trigger = _view.BtnBack.AddComponent<ObservablePointerClickTrigger>();
            trigger
                .OnPointerClickAsObservable()
                .Subscribe(_ =>
                {
                    _playerModel.OnBackBtnTouched();
                })
                .AddTo(this);

            for (int i = 0; i < _view.OptionBtnRoot.childCount; i++)
            {
#nullable enable
                Transform? detectButton = _view.OptionBtnRoot.GetChild(i)?.GetChild(2);
                if (detectButton == null) continue;
#nullable disable
                SubscribeOptionBtnEvent(detectButton, i);
            }
        }

        protected abstract void SubscribeSelectPanel();
    }
}
