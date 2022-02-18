using System.Collections.Generic;
using UniRx;
using System.Linq;
using System;
using Zenject;

namespace HexRPG.Battle.Player
{
    using Stage;

    public interface ISelectSkillController
    {
        void SelectSkill(int index);
        void ResetSelection();
    }

    public interface ISelectSkillObservable
    {
        IReadOnlyReactiveProperty<int> SelectedSkillIndex { get; }
        int DuplicateSelectedCount { get; }
        List<Hex> CurAttackIndicateHexList { get; }
    }

    public class SkillSelecter : ISelectSkillController, ISelectSkillObservable, IInitializable, IDisposable
    {
        ITransformController _transformController;
        IStageController _stageController;
        IMemberObservable _memberObservable;

        CompositeDisposable _disposables = new CompositeDisposable();

        IReadOnlyReactiveProperty<int> ISelectSkillObservable.SelectedSkillIndex => _selectedSkillIndex;
        ReactiveProperty<int> _selectedSkillIndex = new ReactiveProperty<int>(-1);

        int ISelectSkillObservable.DuplicateSelectedCount => _duplicateSelectedCount;
        int _duplicateSelectedCount = 0;

        List<Hex> ISelectSkillObservable.CurAttackIndicateHexList => _curAttackIndicateHexList;
        List<Hex> _curAttackIndicateHexList = new List<Hex>();

        public SkillSelecter(
            ITransformController transformController,
            IStageController stageController,
            IMemberObservable memberObservable
        )
        {
            _transformController = transformController;
            _stageController = stageController;
            _memberObservable = memberObservable;
        }

        void IInitializable.Initialize()
        {
            _selectedSkillIndex
                .Subscribe(index =>
                {
                    _stageController.ResetAttackIndicate(_curAttackIndicateHexList);

                    if (index >= 0)
                    {
                        var skillSetting = _memberObservable.CurMemberSkillList.Value[index].SkillSetting;
                        _curAttackIndicateHexList =
                        _stageController.GetHexList(_transformController.GetLandedHex(), skillSetting.Range, _duplicateSelectedCount).ToList();
                        _stageController.SetAttackIndicate(_curAttackIndicateHexList);
                    }
                })
                .AddTo(_disposables);
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

        void IDisposable.Dispose()
        {
            _disposables.Dispose();
        }
    }
}