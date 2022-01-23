using UnityEngine;
using UnityEngine.UI;
using System;
using UniRx;
using UniRx.Triggers;

namespace HexRPG.Battle.Player.UI
{
    public interface ISelectUI : IFeature
    {
        void RegisterSelectOptionEvent(Action<int> action); // 各ボタンを押したときの挙動登録
        void RegisterBtnBackEvent(Action action); // Backボタンを押したときの挙動登録

        void UpdateOptionBtnSelectedStatus(int index, bool isSelected);
        void UpdateOptionBtnSprite(Sprite[] spriteList); // 各ボタンのSprite更新
    }

    public class SelectUI : AbstractCustomComponentBehaviour, ISelectUI
    {
        [SerializeField] Transform _optionBtnRoot;
        [SerializeField] GameObject _btnBack;

        [SerializeField] Sprite _optionBtnDefaultSprite;
        [SerializeField] Sprite _optionBtnSelectedSprite;

        public override void Register(ICustomComponentCollection owner)
        {
            base.Register(owner);

            owner.RegisterInterface<ISelectUI>(this);
        }

        public override void Initialize()
        {
            base.Initialize();
        }

        void ISelectUI.RegisterSelectOptionEvent(Action<int> action)
        {
            void SetUpSkillBtnEvent(Transform btn, int index)
            {
                ObservablePointerClickTrigger trigger;
                if (!btn.TryGetComponent(out trigger)) trigger = btn.gameObject.AddComponent<ObservablePointerClickTrigger>();
                trigger
                    .OnPointerClickAsObservable()
                    .Subscribe(_ =>
                    {
                        action.Invoke(index);
                    })
                    .AddTo(this);
            }

            for (int i = 0; i < _optionBtnRoot.childCount; i++)
            {
#nullable enable
                Transform? detectButton = _optionBtnRoot.GetChild(i)?.GetChild(2);
                if (detectButton == null) continue;
#nullable disable
                SetUpSkillBtnEvent(detectButton, i);
            }
        }

        void ISelectUI.RegisterBtnBackEvent(Action action)
        {
            ObservablePointerClickTrigger trigger;
            if (!_btnBack.TryGetComponent(out trigger)) trigger = _btnBack.AddComponent<ObservablePointerClickTrigger>();
            trigger
                .OnPointerClickAsObservable()
                .Subscribe(_ => action.Invoke())
                .AddTo(this);
        }

        void ISelectUI.UpdateOptionBtnSelectedStatus(int index, bool isSelected)
        {
            if (isSelected) _optionBtnRoot.GetChild(index).GetChild(0).GetComponent<Image>().sprite = _optionBtnSelectedSprite;
            else _optionBtnRoot.GetChild(index).GetChild(0).GetComponent<Image>().sprite = _optionBtnDefaultSprite;
        }

        void ISelectUI.UpdateOptionBtnSprite(Sprite[] spriteList)
        {
            for (int i = 0; i < _optionBtnRoot.childCount; i++)
            {
                if (i > spriteList.Length - 1) break;
                var optionBtn = _optionBtnRoot.GetChild(i);
                Image icon = optionBtn.GetChild(1).GetComponent<Image>();
                icon.sprite = spriteList[i];
            }
        }
    }
}
