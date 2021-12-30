using UnityEngine;
using UniRx;
using UniRx.Triggers;
using UnityEngine.UI;

namespace HexRPG.Battle.Player.Panel
{
    [RequireComponent(typeof(SelectSkillPanelView))]
    public class SelectSkillPanelPresenter : MonoBehaviour
    {
        SelectSkillPanelView _view;
        PlayerModel _playerModel;

        public void Init(PlayerModel playerModel)
        {
            _view = GetComponent<SelectSkillPanelView>();
            _playerModel = playerModel;

            _view.CloseSelectSkillPanel();

            SubscribeCharacterChange();
            SubscribeSelectSkillPanelEvent();
        }

        void SubscribeCharacterChange()
        {
            _playerModel
                .CurCharacter
                .Subscribe(character =>
                {
                    _view.UpdateSkillBtnList(character);
                })
                .AddTo(this);
        }

        void SubscribeSelectSkillPanelEvent()
        {
            void SubscribeSkillButton(Button button, int index)
            {
                button
                    .OnClickAsObservable()
                    .Subscribe(_ =>
                    {
                        _playerModel.TrySaveSelectedSkillIndex(index);
                    });
            }

            _view.BtnDecide.AddComponent<ObservablePointerClickTrigger>()
                .OnPointerClickAsObservable()
                .Subscribe(_ =>
                {
                    _playerModel.OnDecideButtonTouched();
                })
                .AddTo(this);

            _view.BtnBack.AddComponent<ObservablePointerClickTrigger>()
                .OnPointerClickAsObservable()
                .Subscribe(_ =>
                {
                    _playerModel.OnBackButtonTouched();
                })
                .AddTo(this);

            for(int i = 0; i < _view.SkillBtnListRoot.childCount; i++)
            {
#nullable enable
                Button? detectButton = _view.SkillBtnListRoot.GetChild(i).GetChild(2)?.GetComponent<Button>();
                if (detectButton == null) continue;
#nullable disable
                SubscribeSkillButton(detectButton, i);
            }

            _playerModel
                .CurSelectedSkillIndex
                .Skip(1)
                .Subscribe(index => _view.SetSkillBtnSelectedStatus(index, true))
                .AddTo(this);

            _playerModel
                .ClearSelectedSkillIndex
                .Subscribe(index => _view.SetSkillBtnSelectedStatus(_playerModel.CurSelectedSkillIndex.Value, false))
                .AddTo(this);

            _playerModel
                .CloseSelectSkillPanel
                .Subscribe(_ => _view.CloseSelectSkillPanel())
                .AddTo(this);

            _playerModel
                .OpenSelectSkillPanelToSkillSelect
                .Subscribe(_ => _view.OpenSelectSkillPanelToSkillSelect())
                .AddTo(this);
        }
    }
}
