using UnityEngine;
using UnityEngine.UI;
using System;
using System.Linq;
using System.Threading;
using UniRx;
using UniRx.Triggers;
using Cysharp.Threading.Tasks;

namespace HexRPG.Battle.Player.UI
{
    public class SelectSkillUI : MonoBehaviour, ICharacterUI
    {
        ISelectSkillController _selectSkillController;
        ISelectSkillObservable _selectSkillObservable;

        [SerializeField] Transform _skillBtnList;

        [SerializeField] GameObject _btnBack;
        [SerializeField] GameObject _btnChangeMember;

        Sprite[] _skillIconList = new Sprite[0];

        [SerializeField] Material _skillBackgroundDefaultMat;
        [SerializeField] Material _skillBackgroundSelectedMat;
        [SerializeField] Material _skillIconDefaultMat;
        [SerializeField] Material _skillIconSelectedMat;

        IObservable<Unit> ICharacterUI.OnBack => _onBack;
        ISubject<Unit> _onBack = new Subject<Unit>();

        bool _inOperation = false;

        void Start()
        {
            // �߂�{�^����������Select��Ԃ���������\��
            ObservablePointerClickTrigger trigger;
            if (!_btnBack.TryGetComponent(out trigger)) trigger = _btnBack.AddComponent<ObservablePointerClickTrigger>();
            trigger
                .OnPointerClickAsObservable()
                .Subscribe(_ =>
                {
                    if (!_inOperation) return;
                    CancelSelection();
                    _onBack.OnNext(Unit.Default);
                })
                .AddTo(this);

            // Member�I���{�^��
            if (!_btnChangeMember.TryGetComponent(out trigger)) trigger = _btnChangeMember.AddComponent<ObservablePointerClickTrigger>();
            trigger
                .OnPointerClickAsObservable()
                .Subscribe(_ =>
                {
                    CancelSelection();
                })
                .AddTo(this);
        }

        void ICharacterUI.Bind(ICharacterComponentCollection chara)
        {
            if (chara is IPlayerComponentCollection playerOwner)
            {
                _selectSkillController = playerOwner.SelectSkillController;
                _selectSkillObservable = playerOwner.SelectSkillObservable;

                var memberObservable = playerOwner.MemberObservable;
                memberObservable.CurMemberSkillList
                    .Subscribe(skillList =>
                    {
                        _skillIconList = skillList.Select(skill =>
                        {
                            return skill.SkillSetting.Icon;
                        }).ToArray();

                        UpdateBtnIcon();
                    })
                    .AddTo(this);

                // �e�{�^���^�b�v�C�x���g�o�^
                SetupSkillSelectEvent();

                _selectSkillObservable.SelectedSkillIndex
                    .Pairwise()
                    .Subscribe(x =>
                    {
                        if (x.Current != x.Previous && x.Previous >= 0)
                        {
                            UpdateBtnSelectedStatus(x.Previous, false);
                        }

                        if (x.Current >= 0)
                        {
                            UpdateBtnSelectedStatus(x.Current, true);
                        }
                    })
                    .AddTo(this);

                // Skill����������Select��Ԃ�����
                playerOwner.SkillObservable.OnStartSkill
                    .DelayFrame(1) // Skill���s����Player�̍U���͈͓���Enemy�����邩�ǂ������肷��K�v�����邽�߁A1F���attackRange��Hex����reservation��remove����
                    .Subscribe(_ => {
                        CancelSelection();
                    })
                    .AddTo(this);
            }
        }

        void ICharacterUI.SwitchOperation(bool inOperation)
        {
            InternalSwitchOperation(inOperation).Forget();
        }

        async UniTaskVoid InternalSwitchOperation(bool inOperation)
        {
            //! MemberSelect��Ԃ�back�{�^���^�b�v���ɂ���SelectSkillUI(this)��_inOperation��true�ɂ��Ă��܂��ƁA
            //! SelectSkillUI(this)��back�{�^���^�b�v���̃C�x���g�����s����Ă��܂�����1F�x�点��
            await UniTask.Yield(this.GetCancellationTokenOnDestroy());
            _inOperation = inOperation;
        }

        void UpdateBtnIcon()
        {
            for (int i = 0; i < _skillBtnList.childCount; i++)
            {
                int index = _skillBtnList.childCount - 1 - i;
                if (index > _skillIconList.Length - 1)
                {
                    _skillBtnList.GetChild(i).gameObject.SetActive(false);
                }
                else
                {
                    var optionBtn = _skillBtnList.GetChild(i);
                    Image icon = optionBtn.GetChild(0).GetChild(0).GetComponent<Image>();
                    icon.sprite = _skillIconList[index];

                    _skillBtnList.GetChild(i).gameObject.SetActive(true);
                }
            }
        }

        void SetupSkillSelectEvent()
        {
            void SetUpSkillBtnEvent(Transform btn, int index)
            {
                ObservablePointerClickTrigger trigger;
                if (!btn.TryGetComponent(out trigger)) trigger = btn.gameObject.AddComponent<ObservablePointerClickTrigger>();
                trigger
                    .OnPointerClickAsObservable()
                    .Subscribe(_ =>
                    {
                        _selectSkillController.SelectSkill(index);
                    })
                    .AddTo(this);
            }
            for (int i = 0; i < _skillBtnList.childCount; i++)
            {
#nullable enable
                Transform? detectButton = _skillBtnList.GetChild(i).GetChild(0);
                if (detectButton == null) continue;
#nullable disable
                int index = _skillBtnList.childCount - 1 - i;
                SetUpSkillBtnEvent(detectButton, index);
            }
        }

        void UpdateBtnSelectedStatus(int index, bool isSelected)
        {
            index = _skillBtnList.childCount - 1 - index;
            if (isSelected)
            {
                _skillBtnList.GetChild(index).GetChild(0).GetComponent<Image>().material = _skillBackgroundSelectedMat;
                _skillBtnList.GetChild(index).GetChild(0).GetChild(0).GetComponent<Image>().material = _skillIconSelectedMat;
            }
            else
            {
                _skillBtnList.GetChild(index).GetChild(0).GetComponent<Image>().material = _skillBackgroundDefaultMat;
                _skillBtnList.GetChild(index).GetChild(0).GetChild(0).GetComponent<Image>().material = _skillIconDefaultMat;
            }
        }

        void CancelSelection()
        {
            int index = _selectSkillObservable.SelectedSkillIndex.Value;
            if(index >= 0)
            {
                UpdateBtnSelectedStatus(index, false);
                _selectSkillController.ResetSelection();
            }
        }
    }
}
