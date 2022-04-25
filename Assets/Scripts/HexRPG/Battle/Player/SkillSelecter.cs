using UnityEngine;
using UniRx;
using System;
using Zenject;

namespace HexRPG.Battle.Player
{
    using Skill;
    using Stage;

    public interface ISelectSkillController
    {
        void SelectSkill(int index);
        void ResetSelection();
    }

    public interface ISelectSkillObservable
    {
        IReadOnlyReactiveProperty<int> SelectedSkillIndex { get; }
        int SelectedSkillRotation { get; }
    }

    public class SkillSelecter : ISelectSkillController, ISelectSkillObservable, IInitializable, IDisposable
    {
        ITransformController _transformController;
        IAttackReserve _attackReserve;
        IMemberObservable _memberObservable;
        IStageController _stageController;

        CompositeDisposable _disposables = new CompositeDisposable();

        IReadOnlyReactiveProperty<int> ISelectSkillObservable.SelectedSkillIndex => _selectedSkillIndex;
        readonly ReactiveProperty<int> _selectedSkillIndex = new ReactiveProperty<int>(-1);

        int ISelectSkillObservable.SelectedSkillRotation => _selectedSkillRotation;
        int _selectedSkillRotation = 0;

        int _duplicateSelectedCount = 0;

        public SkillSelecter(
            ITransformController transformController,
            IAttackReserve attackReserve,
            IMemberObservable memberObservable,
            IStageController stageController
        )
        {
            _transformController = transformController;
            _attackReserve = attackReserve;
            _memberObservable = memberObservable;
            _stageController = stageController;
        }

        void IInitializable.Initialize()
        {
            _selectedSkillIndex
                .Subscribe(index =>
                {
                    _attackReserve.FinishAttackReservation();

                    if (index >= 0)
                    {
                        var attackRange = _memberObservable.CurMember.Value.SkillSpawnObservable.SkillList[index].Skill.FullAttackRange;
                        _selectedSkillRotation = (_transformController.DefaultRotation + 30) / 60 * 60 + _duplicateSelectedCount * 60;

                        var curAttackIndicateHexList =
                            _stageController.GetHexList(_transformController.GetLandedHex(), attackRange, _selectedSkillRotation);
                        _attackReserve.StartAttackReservation(curAttackIndicateHexList, _memberObservable.CurMember.Value);
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