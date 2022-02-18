using UnityEngine;
using System;
using System.Linq;
using UniRx;
using UniRx.Triggers;

namespace HexRPG.Battle.Player.UI
{
    public class SelectSkillUI : MonoBehaviour, ICharacterUI
    {
        ISelectUI _selectUI;

        ISelectSkillController _selectSkillController;
        ISelectSkillObservable _selectSkillObservable;

        [SerializeField] GameObject _btnChangeMember;

        IObservable<Unit> ICharacterUI.OnBack => _onBack;
        ISubject<Unit> _onBack = new Subject<Unit>();

        void Awake()
        {
            _selectUI = GetComponent<ISelectUI>();
        }

        void Start()
        {
            // 戻るボタン押したらSelect状態を解除し非表示
            _selectUI.RegisterBtnBackEvent(() =>
            {
                CancelSelection();
                _onBack.OnNext(Unit.Default);
            });

            // Member選択ボタン
            ObservablePointerClickTrigger trigger;
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
            Sprite[] skillIconList = new Sprite[0];

            if (chara is IPlayerComponentCollection playerOwner)
            {
                _selectSkillController = playerOwner.SelectSkillController;
                _selectSkillObservable = playerOwner.SelectSkillObservable;

                var memberObservable = playerOwner.MemberObservable;
                memberObservable.CurMemberSkillList
                    .Subscribe(skillList =>
                    {
                        skillIconList = skillList.Select(skill =>
                        {
                            return skill.SkillSetting.Icon;
                        }).ToArray();
                        _selectUI.UpdateOptionBtnSprite(skillIconList);
                    })
                    .AddTo(this);

                // 各ボタンタップイベント登録
                _selectUI.RegisterSelectOptionEvent(index =>
                {
                    if (index > skillIconList.Length - 1) return;
                    _selectSkillController.SelectSkill(index);
                });

                _selectSkillObservable.SelectedSkillIndex
                    .Pairwise()
                    .Subscribe(x =>
                    {
                        if (x.Current != x.Previous && x.Previous >= 0)
                        {
                            _selectUI.UpdateOptionBtnSelectedStatus(x.Previous, false);
                        }

                        if (x.Current >= 0)
                        {
                            _selectUI.UpdateOptionBtnSelectedStatus(x.Current, true);
                        }
                    })
                    .AddTo(this);

                // Skill発動したらSelect状態を解除
                playerOwner.SkillObservable.OnStartSkill
                    .Subscribe(_ => {
                        CancelSelection();
                    })
                    .AddTo(this);
            }
        }

        void ICharacterUI.SwitchShow(bool isShow)
        {
            gameObject.SetActive(isShow);
        }

        void CancelSelection()
        {
            int index = _selectSkillObservable.SelectedSkillIndex.Value;
            if(index >= 0)
            {
                _selectUI.UpdateOptionBtnSelectedStatus(index, false);
                _selectSkillController.ResetSelection();
            }
        }
    }
}
