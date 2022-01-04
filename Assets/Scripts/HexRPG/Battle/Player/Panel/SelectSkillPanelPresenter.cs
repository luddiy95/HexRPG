using UnityEngine;
using UniRx;
using UniRx.Triggers;

namespace HexRPG.Battle.Player.Panel
{
    [RequireComponent(typeof(SelectSkillPanelView))]
    public class SelectSkillPanelPresenter : SelectBasePanelPresenter
    {
        public override void Init(PlayerModel playerModel)
        {
            base.Init(playerModel);
        }

        protected override void OnCharacterChanged(Character.Character character)
        {
            if (_view is SelectSkillPanelView selectSkillPanelView) selectSkillPanelView.UpdateSkillBtnList(character);
        }

        protected override void OnOptionBtnSelected(int index)
        {
            _playerModel.TrySaveSelectedSkillIndex(index);
        }

        protected override void SubscribeSelectPanelEvent()
        {
            base.SubscribeSelectPanelEvent();

            if (!(_view is SelectSkillPanelView selectSkillPanelView)) return;
            selectSkillPanelView.BtnChangeCharacter.AddComponent<ObservablePointerClickTrigger>()
                .OnPointerClickAsObservable()
                .Subscribe(_ => _playerModel.OnChangeCharacterBtnTouched())
                .AddTo(this);
        }

        protected override void SubscribeSelectPanel()
        {
            _playerModel
                .CurSelectedSkillIndex
                .Skip(1)
                .Subscribe(index => _view.SetOptionBtnSelectedStatus(index, true))
                .AddTo(this);

            _playerModel
                .ClearSelectedSkillIndex
                .Subscribe(_ => _view.SetOptionBtnSelectedStatus(_playerModel.CurSelectedSkillIndex.Value, false))
                .AddTo(this);

            _playerModel
                .CloseSelectSkillPanel
                .Subscribe(_ => _view.CloseSelectPanel())
                .AddTo(this);

            _playerModel
                .OpenSelectSkillPanel
                .Subscribe(_ => _view.OpenSelectPanel())
                .AddTo(this);
        }
    }
}
