using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UniRx;
using UniRx.Triggers;
using Zenject;

namespace HexRPG.Battle.UI
{
    public class UIManager : MonoBehaviour
    {
        BattleData _battleData;
        IBattleObservable _battleObservable;
        IPauseController _pauseController;
        IPauseObservable _pauseObservable;

        [Header("Skill��UI�����I�u�W�F�N�g")]
        [SerializeField] GameObject _skillList;
        [Header("Member��UI�����I�u�W�F�N�g")]
        [SerializeField] GameObject _memberList;
        ICharacterUI _skillListUI;
        ICharacterUI _memberListUI;

        [Header("�{�^��")]
        [SerializeField] Image _btnFire;
        [SerializeField] Image _btnChangeMember;
        [SerializeField] GameObject _btnBack;

        [Header("Transform���[�g")]
        [SerializeField] Transform _pauseGrayBackRoot;
        [SerializeField] GameObject _pauseGray;
        [SerializeField] Transform _pauseGrayFrontRoot;

        [Header("Sprite")]
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
            _skillListUI = _skillList.GetComponent<ICharacterUI>();
            _memberListUI = _memberList.GetComponent<ICharacterUI>();

            _battleObservable.OnPlayerSpawn
                .Skip(1)
                .Subscribe(playerOwner =>
                {
                    new List<ICharacterUI>
                    {
                        _skillListUI,
                        _memberListUI
                    }
                    .ForEach(ui =>
                    {
                        ui.Bind(playerOwner);
                    });

                    // Pause��
                    _pauseObservable.OnPause
                        .Subscribe(_ =>
                        {
                            _skillList.SetActive(true);
                            SwitchOperation(OPERATION.SELECT_SKILL);
                            SwitchPause(true);
                        })
                        .AddTo(this);

                    // PlayerSkill�J�n��
                    playerOwner.SkillObservable.OnStartSkill
                        .Subscribe(_ => {
                            _skillList.SetActive(false);
                            SwitchOperation(OPERATION.NONE);
                            SwitchPause(false);
                        })
                        .AddTo(this);

                    // Member�ύX�{�^���^�b�v��
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

                    // Member�I��Back��
                    _memberListUI.OnBack
                        .Subscribe(_ => {
                            SwitchOperation(OPERATION.SELECT_SKILL);
                            SwitchBtnMemberChangeEneble(true);
                        })
                        .AddTo(this);

                    // Skill�I��Back��&PlayerSkill�I����
                    Observable.Merge(_skillListUI.OnBack, playerOwner.SkillObservable.OnFinishSkill)
                        .Subscribe(_ =>
                        {
                            _skillList.SetActive(false);
                            SwitchOperation(OPERATION.NONE);
                            SwitchPause(false);
                            _pauseController.Restart();
                        })
                        .AddTo(this);

                    // Player�X�e�[�g�J�ڎ�
                    playerOwner.ActionStateObservable.CurrentState
                        .Subscribe(state =>
                        {
                            SwitchBtnActionEnable(state.Type == ActionStateType.IDLE || state.Type == ActionStateType.PAUSE);
                        })
                        .AddTo(this);
                });

            SwitchPause(false);
            _skillList.SetActive(false);
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
                    _skillListUI.SwitchOperation(false);
                    _memberListUI.SwitchOperation(false);
                    _btnFire.sprite = _btnPauseSprite;
                    break;
                case OPERATION.SELECT_SKILL:
                    _skillListUI.SwitchOperation(true);
                    _memberListUI.SwitchOperation(false);

                    _skillList.transform.SetParent(_pauseGrayFrontRoot);
                    _memberList.transform.SetParent(_pauseGrayBackRoot);

                    _btnFire.sprite = _btnActionSprite;
                    break;
                case OPERATION.SELECT_MEMBER:
                    _skillListUI.SwitchOperation(false);
                    _memberListUI.SwitchOperation(true);

                    _skillList.transform.SetParent(_pauseGrayBackRoot);
                    _memberList.transform.SetParent(_pauseGrayFrontRoot);

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
            //! BtnMemberChange��enable�o��������SelectMemberUI�ōs��Ȃ�(UIManager�Ŋ���btnChangeMember���Q�Ƃ��Ă��邩�炻����g��)
            if (enable) _btnChangeMember.material = _battleData.SubBtnEnableMat;
            else _btnChangeMember.material = _battleData.SubBtnDisableMat;
        }

        #endregion
    }
}
