using UnityEngine;
using System;
using System.Linq;
using UniRx;
using UniRx.Triggers;

namespace HexRPG.Battle.Player.UI
{
    using Skill;

    public class SelectSkillUI : AbstractCustomComponentBehaviour, ICharacterUI
    {
        ISelectUI _selectUI;

        ISelectSkillController _selectSkillController;
        ISelectSkillObservable _selectSkillObservable;

        [SerializeField] GameObject _btnChangeMember;

        IObservable<Unit> ICharacterUI.OnBack => _onBack;
        ISubject<Unit> _onBack = new Subject<Unit>();

        public override void Register(ICustomComponentCollection owner)
        {
            base.Register(owner);

            owner.RegisterInterface<ICharacterUI>(this);
        }

        public override void Initialize()
        {
            base.Initialize();

            Owner.QueryInterface(out _selectUI);

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

        void ICharacterUI.Bind(ICustomComponentCollection character)
        {
            Sprite[] skillIconList = new Sprite[0];

            if (character.QueryInterface(out IMemberObservable memberObservable))
            {
                memberObservable.CurMember
                    .Where(member => member != null)
                    .Subscribe(_ =>
                    {
                        skillIconList = memberObservable.CurMemberSkillList.Select(skill =>
                        {
                            skill.QueryInterface(out ISkillSetting skillSetting);
                            return skillSetting.Icon;
                        }).ToArray();
                        _selectUI.UpdateOptionBtnSprite(skillIconList);
                    })
                    .AddTo(this);
            }

            // 各ボタンタップイベント登録
            if (character.QueryInterface(out _selectSkillController))
            {
                _selectUI.RegisterSelectOptionEvent(index =>
                {
                    if (index > skillIconList.Length - 1) return;
                    _selectSkillController.SelectSkill(index);
                });
            }

            if (character.QueryInterface(out _selectSkillObservable))
            {
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
            }

            if (character.QueryInterface(out ISkillObservable skillObservable))
            {
                // Skill発動したらSelect状態を解除
                skillObservable.OnStartSkill
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
