using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UniRx;
using UniRx.Triggers;
using Zenject;

namespace HexRPG.Battle
{
    public class UIManager : MonoBehaviour
    {
        BattleData _battleData;
        IBattleObservable _battleObservable;
        IPauseController _pauseController;
        IPauseObservable _pauseObservable;

        [SerializeField] GameObject _selectSkillObj;
        [SerializeField] GameObject _selectMemberObj;
        ICharacterUI _selectSkillUI;
        ICharacterUI _selectMemberUI;

        [SerializeField] Image _btnFire;
        [SerializeField] Image _btnChangeMember;
        [SerializeField] GameObject _btnBack;

        [SerializeField] Transform _pauseGrayBack;
        [SerializeField] GameObject _pauseGray;
        [SerializeField] Transform _pauseGrayFront;

        [SerializeField] Sprite _btnPauseSprite;
        [SerializeField] Sprite _btnActionSprite;
        [SerializeField] Sprite _btnChangeSprite;

        [Inject]
        public void Construct(
            BattleData battleData,
            IBattleObservable battleObservable,
            IPauseController pauseController,
            IPauseObservable pauseObservable
        )
        {
            _battleData = battleData;
            _battleObservable = battleObservable;
            _pauseController = pauseController;
            _pauseObservable = pauseObservable;
        }

        void Start()
        {
            _selectSkillUI = _selectSkillObj.GetComponent<ICharacterUI>();
            _selectMemberUI = _selectMemberObj.GetComponent<ICharacterUI>();

            _battleObservable.OnPlayerSpawn
                .Skip(1)
                .Subscribe(playerOwner =>
                {
                    new List<ICharacterUI>
                    {
                        _selectSkillUI,
                        _selectMemberUI
                    }
                    .ForEach(ui =>
                    {
                        ui.Bind(playerOwner);
                    });

                    // Pause時
                    _pauseObservable.OnPause
                        .Subscribe(_ =>
                        {
                            _selectSkillObj.SetActive(true);
                            SwitchOperation(OPERATION.SELECT_SKILL);
                            SwitchPause(true);
                        })
                        .AddTo(this);

                    // PlayerSkill開始時
                    playerOwner.SkillObservable.OnStartSkill
                        .Subscribe(_ => {
                            _selectSkillObj.SetActive(false);
                            SwitchOperation(OPERATION.NONE);
                            SwitchPause(false);
                        })
                        .AddTo(this);

                    // Member変更ボタンタップ時
                    ObservablePointerClickTrigger trigger;
                    if (!_btnChangeMember.TryGetComponent(out trigger)) trigger = _btnChangeMember.gameObject.AddComponent<ObservablePointerClickTrigger>();
                    trigger
                        .OnPointerClickAsObservable()
                        .Subscribe(_ =>
                        {
                            SwitchOperation(OPERATION.SELECT_MEMBER);
                            SwitchBtnMemberChangeEneble(false);
                        })
                        .AddTo(this);

                    // Member選択Back時
                    _selectMemberUI.OnBack
                        .Subscribe(_ => {
                            SwitchOperation(OPERATION.SELECT_SKILL);
                            SwitchBtnMemberChangeEneble(true);
                        })
                        .AddTo(this);

                    // Skill選択Back時&PlayerSkill終了時
                    Observable.Merge(_selectSkillUI.OnBack, playerOwner.SkillObservable.OnFinishSkill)
                        .Subscribe(_ =>
                        {
                            SwitchOperation(OPERATION.NONE);
                            SwitchPause(false);
                            _pauseController.Restart();
                        })
                        .AddTo(this);

                    // Playerステート遷移時
                    playerOwner.ActionStateObservable.CurrentState
                        .Subscribe(state =>
                        {
                            SwitchBtnActionEnable(state.Type == ActionStateType.IDLE || state.Type == ActionStateType.PAUSE);
                        })
                        .AddTo(this);
                });

            SwitchPause(false);
            _selectSkillObj.SetActive(false);
        }

        #region View

        enum OPERATION
        {
            NONE,
            SELECT_SKILL,
            SELECT_MEMBER
        }
        void SwitchOperation(OPERATION operation)
        {
            switch (operation)
            {
                case OPERATION.NONE:
                    _selectSkillUI.SwitchOperation(false);
                    _selectMemberUI.SwitchOperation(false);
                    _btnFire.sprite = _btnPauseSprite;
                    break;
                case OPERATION.SELECT_SKILL:
                    _selectSkillUI.SwitchOperation(true);
                    _selectMemberUI.SwitchOperation(false);

                    _selectSkillObj.transform.SetParent(_pauseGrayFront);
                    _selectMemberObj.transform.SetParent(_pauseGrayBack);

                    _btnFire.sprite = _btnActionSprite;
                    break;
                case OPERATION.SELECT_MEMBER:
                    _selectSkillUI.SwitchOperation(false);
                    _selectMemberUI.SwitchOperation(true);

                    _selectSkillObj.transform.SetParent(_pauseGrayBack);
                    _selectMemberObj.transform.SetParent(_pauseGrayFront);

                    _btnFire.sprite = _btnChangeSprite;
                    break;
            }
        }


        void SwitchPause(bool isPause)
        {
            _btnBack.SetActive(isPause);
            _btnChangeMember.gameObject.SetActive(isPause);
            _pauseGray.SetActive(isPause);
        }

        void SwitchBtnActionEnable(bool enable)
        {                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                        
            if (enable) _btnFire.material = _battleData.MainBtnEnableMat;
            else _btnFire.material = _battleData.MainBtnDisableMat;
        }

        void SwitchBtnMemberChangeEneble(bool enable)
        {
            //! BtnMemberChangeのenable出し分けはSelectMemberUIで行わない(UIManagerで既にbtnChangeMemberを参照しているからそれを使う)
            if (enable) _btnChangeMember.material = _battleData.SubBtnEnableMat;
            else _btnChangeMember.material = _battleData.SubBtnDisableMat;
        }

        #endregion
    }
}
