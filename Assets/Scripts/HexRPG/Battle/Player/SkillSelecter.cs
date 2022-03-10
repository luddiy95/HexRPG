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
        ICharacterComponentCollection _characterComponentCollection;
        ITransformController _transformController;
        IAttackReserve _attackReserve;
        IMemberObservable _memberObservable;
        IStageController _stageController;

        CompositeDisposable _disposables = new CompositeDisposable();

        IReadOnlyReactiveProperty<int> ISelectSkillObservable.SelectedSkillIndex => _selectedSkillIndex;
        ReactiveProperty<int> _selectedSkillIndex = new ReactiveProperty<int>(-1);

        int ISelectSkillObservable.DuplicateSelectedCount => _duplicateSelectedCount;
        int _duplicateSelectedCount = 0;

        List<Hex> ISelectSkillObservable.CurAttackIndicateHexList => _curAttackIndicateHexList;
        List<Hex> _curAttackIndicateHexList = new List<Hex>();

        public SkillSelecter(
            ICharacterComponentCollection characterComponentCollection,
            ITransformController transformController,
            IAttackReserve attackReserve,
            IMemberObservable memberObservable,
            IStageController stageController
        )
        {
            _characterComponentCollection = characterComponentCollection;
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
                        var skillSetting = _memberObservable.CurMemberSkillList.Value[index].SkillSetting;
                        _curAttackIndicateHexList =
                            _stageController.GetHexList(_transformController.GetLandedHex(), skillSetting.Range, _duplicateSelectedCount).ToList();
                        _attackReserve.StartAttackReservation(_curAttackIndicateHexList, _characterComponentCollection);
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