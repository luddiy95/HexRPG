using System.Collections.Generic;
using UnityEngine;
using UniRx;
using UniRx.Triggers;
using Zenject;

namespace HexRPG.Battle
{
    public class UIManager : MonoBehaviour
    {
        IBattleObservable _battleObservable;

        [Header("決定ボタン")]
        [SerializeField] GameObject _btnFire;

        [Header("Skill選択UI")]
        [SerializeField] GameObject _selectSkillUI;

        [Header("Member選択UI")]
        [SerializeField] GameObject _selectMemberUI;

        [Header("Member選択ボタン")]
        [SerializeField] GameObject _btnChangeMember;

        [Inject]
        public void Construct(IBattleObservable battleObservable)
        {
            _battleObservable = battleObservable;
        }

        void Start()
        {
            var selectSkillUI = _selectSkillUI.GetComponent<ICharacterUI>();
            var selectMemberUI = _selectMemberUI.GetComponent<ICharacterUI>();

            _battleObservable.OnPlayerSpawn
                .Subscribe(playerOwner =>
                {
                    new List<ICharacterUI>
                    {
                        selectSkillUI,
                        selectMemberUI
                    }
                    .ForEach(ui =>
                    {
                        ui.Bind(playerOwner);
                    });

                    playerOwner.PauseObservable.OnPause
                        .Subscribe(_ =>
                        {
                            selectSkillUI.SwitchShow(true);
                            SwitchPause(isPause: true);
                        })
                        .AddTo(this);

                    Observable.Merge(selectSkillUI.OnBack, playerOwner.SkillObservable.OnFinishSkill)
                        .Subscribe(_ =>
                        {
                            selectSkillUI.SwitchShow(false);
                            SwitchPause(isPause: false);
                            playerOwner.PauseController.Restart();
                        })
                        .AddTo(this);
                });

            selectMemberUI.OnBack
                .Subscribe(_ =>
                {
                    selectMemberUI.SwitchShow(false);
                    selectSkillUI.SwitchShow(true);
                })
                .AddTo(this);

            // Member選択ボタン
            ObservablePointerClickTrigger trigger;
            if (!_btnChangeMember.TryGetComponent(out trigger)) trigger = _btnChangeMember.AddComponent<ObservablePointerClickTrigger>();
            trigger
                .OnPointerClickAsObservable()
                .Subscribe(_ =>
                {
                    selectMemberUI.SwitchShow(true);
                    selectSkillUI.SwitchShow(false);
                })
                .AddTo(this);


            SwitchPause(false);
            new List<ICharacterUI>
            {
                selectSkillUI,
                selectMemberUI
            }
            .ForEach(ui =>
            {
                ui.SwitchShow(false);
            });
        }

        #region View

        void SwitchPause(bool isPause)
        {
            if (isPause) _btnFire.transform.localScale = Vector3.one;
            else _btnFire.transform.localScale = 1.8f * Vector3.one;
        }

        #endregion
    }
}
