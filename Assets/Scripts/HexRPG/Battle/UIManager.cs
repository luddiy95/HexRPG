using System.Collections.Generic;
using UnityEngine;
using UniRx;
using UniRx.Triggers;

namespace HexRPG.Battle
{
    using Player;

    public class UIManager : AbstractCustomComponentBehaviour
    {
        [Header("決定ボタン")]
        [SerializeField] GameObject _btnFire;

        [Header("Skill選択UI")]
        [SerializeField] GameObject _selectSkillUI;

        [Header("Member選択UI")]
        [SerializeField] GameObject _selectMemberUI;

        [Header("Member選択ボタン")]
        [SerializeField] GameObject _btnChangeMember;

        public override void Initialize()
        {
            base.Initialize();

            // Owner = 自分自身ComponentCollection
            if (!Owner.QueryInterface(out IComponentCollectionFactory factory)) return;

            var selectSkillUICollection = factory.CreateComponentCollectionWithoutInstantiate(_selectSkillUI, null, null);
            selectSkillUICollection.QueryInterface(out ICharacterUI selectSkillUI);
            var selectMemberUICollection = factory.CreateComponentCollectionWithoutInstantiate(_selectMemberUI, null, null);
            selectMemberUICollection.QueryInterface(out ICharacterUI selectMemberUI);

            if (Owner.QueryInterface(out IBattleObservable battleObservable))
            {
                battleObservable.OnPlayerSpawn
                    .Subscribe(player =>
                    {
                        new List<ICharacterUI>
                        {
                            selectSkillUI,
                            selectMemberUI
                        }
                        .ForEach(ui =>
                        {
                            ui.Bind(player);
                        });

                        if (player.QueryInterface(out IPauseObservable pauseObservable))
                        {
                            pauseObservable.OnPause
                                .Subscribe(_ =>
                                {
                                    selectSkillUI.SwitchShow(true);
                                    SwitchPause(isPause: true);
                                })
                                .AddTo(this);
                        }

                        if (player.QueryInterface(out IPauseController pauseController) && player.QueryInterface(out ISkillObservable skillObservable))
                        {
                            Observable.Merge(selectSkillUI.OnBack, skillObservable.OnFinishSkill)
                                .Subscribe(_ =>
                                {
                                    selectSkillUI.SwitchShow(false);
                                    SwitchPause(isPause: false);
                                    pauseController.Restart();
                                })
                                .AddTo(this);
                        }
                    });
            }

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
