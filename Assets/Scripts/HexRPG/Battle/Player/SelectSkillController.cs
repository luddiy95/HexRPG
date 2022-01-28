using System.Collections.Generic;
using UniRx;
using System.Linq;

namespace HexRPG.Battle.Player
{
    using Skill;
    using Stage;

    public interface ISelectSkillController : IFeature
    {
        void SelectSkill(int index);
        void ResetSelection();
    }

    public interface ISelectSkillObservable : IFeature
    {
        IReadOnlyReactiveProperty<int> SelectedSkillIndex { get; }
        int DuplicateSelectedCount { get; }
        List<Hex> CurAttackIndicateHexList { get; }
    }

    public class SelectSkillController : AbstractCustomComponentBehaviour, ISelectSkillController, ISelectSkillObservable
    {
        IStageController _stageController;
        IMemberObservable _memberObservable;
        ITransformController _transformController;

        IReadOnlyReactiveProperty<int> ISelectSkillObservable.SelectedSkillIndex => _selectedSkillIndex;
        ReactiveProperty<int> _selectedSkillIndex = new ReactiveProperty<int>(-1);

        int ISelectSkillObservable.DuplicateSelectedCount => _duplicateSelectedCount;
        int _duplicateSelectedCount = 0;

        List<Hex> ISelectSkillObservable.CurAttackIndicateHexList => _curAttackIndicateHexList;
        List<Hex> _curAttackIndicateHexList = new List<Hex>();

        public override void Register(ICustomComponentCollection owner)
        {
            base.Register(owner);

            owner.RegisterInterface<ISelectSkillController>(this);
            owner.RegisterInterface<ISelectSkillObservable>(this);
        }

        public override void Initialize()
        {
            base.Initialize();

            Owner.QueryInterface(out _stageController);
            Owner.QueryInterface(out _memberObservable);
            Owner.QueryInterface(out _transformController);

            _selectedSkillIndex
                .Subscribe(index =>
                {
                    _stageController.ResetAttackIndicate(_curAttackIndicateHexList);

                    if(index >= 0)
                    {
                        _memberObservable.CurMemberSkillList[index].QueryInterface(out ISkillSetting skill);
                        _curAttackIndicateHexList = _stageController.GetHexList(_transformController.GetLandedHex(), skill.Range, _duplicateSelectedCount).ToList();
                        _stageController.SetAttackIndicate(_curAttackIndicateHexList);
                    }
                })
                .AddTo(this);

            if(Owner.QueryInterface(out ISkillObservable skillObservable))
            {
                skillObservable.OnFinishSkill
                    .Subscribe(_ =>
                    {
                        _stageController.Liberate(_curAttackIndicateHexList, true);
                    })
                    .AddTo(this);
            }
        }

        void ISelectSkillController.SelectSkill(int index)
        {
            if (_selectedSkillIndex.Value == index) ++_duplicateSelectedCount;
            else _duplicateSelectedCount = 0;
            _selectedSkillIndex.SetValueAndForceNotify(index);
        }

        void ISelectSkillController.ResetSelection()
        {
            _selectedSkillIndex.Value = -1;
            _duplicateSelectedCount = 0;
        }
    }
}