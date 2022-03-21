using System.Collections.Generic;
using UniRx;
using System.Linq;
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
        List<Hex> CurAttackIndicateHexList { get; }
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

        List<Hex> ISelectSkillObservable.CurAttackIndicateHexList => _curAttackIndicateHexList;
        List<Hex> _curAttackIndicateHexList = new List<Hex>();

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
                        var skillSetting = _memberObservable.CurMember.Value.SkillSpawnObservable.SkillList[index].SkillSetting;
                        _selectedSkillRotation = (_transformController.DefaultRotation + 30) / 60 * 60 + _duplicateSelectedCount * 60;
                        _curAttackIndicateHexList =
                            _stageController.GetHexList(
                                _transformController.GetLandedHex(), 
                                skillSetting.Range,
                                _selectedSkillRotation).ToList();
                        _attackReserve.StartAttackReservation(_curAttackIndicateHexList, _memberObservable.CurMember.Value);
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