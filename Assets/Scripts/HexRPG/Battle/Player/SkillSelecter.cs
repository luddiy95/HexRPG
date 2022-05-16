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

        Hex SkillCenter { get; } //! SkillCenterType=NEAREST_ENEMYのとき、PlayerはReservationから実行までラグがあるため「Reservation時の」SkillCenterを取得する必要あり
    }

    public class SkillSelecter : ISelectSkillController, ISelectSkillObservable, IInitializable, IDisposable
    {
        IBattleObservable _battleObservable;
        ITransformController _transformController;
        IAttackReserve _attackReserve;
        IMemberObservable _memberObservable;
        IStageController _stageController;

        CompositeDisposable _disposables = new CompositeDisposable();

        IReadOnlyReactiveProperty<int> ISelectSkillObservable.SelectedSkillIndex => _selectedSkillIndex;
        readonly ReactiveProperty<int> _selectedSkillIndex = new ReactiveProperty<int>(-1);

        int ISelectSkillObservable.SelectedSkillRotation => _selectedSkillRotation; //! SelectedSkillRotation: -179 ～ 180
        int _selectedSkillRotation = 0;

        Hex ISelectSkillObservable.SkillCenter => _skillCenter;
        Hex _skillCenter;

        int _duplicateSelectedCount = 0;

        public SkillSelecter(
            IBattleObservable battleObservable,
            ITransformController transformController,
            IAttackReserve attackReserve,
            IMemberObservable memberObservable,
            IStageController stageController
        )
        {
            _battleObservable = battleObservable;
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
                        var skill = _memberObservable.CurMember.Value.SkillSpawnObservable.SkillList[index].Skill;

                        switch (skill.SkillCenterType)
                        {
                            case SkillCenterType.SELF:
                                _selectedSkillRotation = _duplicateSelectedCount * 60;
                                if (_selectedSkillRotation > 180) _selectedSkillRotation -= 360;

                                _skillCenter = _stageController.GetHex(
                                    _transformController.GetLandedHex(),
                                    skill.SkillCenter,
                                    _transformController.DefaultRotation + _selectedSkillRotation);

                                break;
                            case SkillCenterType.EIM_ENEMY:
                                var enemyList = _battleObservable.EnemyList;
                                //TODO: Enemyがいなかった場合
                                _skillCenter = enemyList[_duplicateSelectedCount % enemyList.Count].TransformController.GetLandedHex();
                                _selectedSkillRotation = _transformController.GetLookRotationAngleY(_skillCenter.transform.position);

                                break;
                        }

                        var curAttackIndicateHexList =
                            _stageController.GetHexList(
                                _skillCenter,
                                skill.FullAttackRange,
                                _transformController.DefaultRotation + _selectedSkillRotation);

                        _attackReserve.StartAttackReservation(curAttackIndicateHexList, _memberObservable.CurMember.Value);
                    }
                })
                .AddTo(_disposables);
        }

        void ISelectSkillController.SelectSkill(int index)
        {
            if (_selectedSkillIndex.Value == index) _duplicateSelectedCount = (_duplicateSelectedCount + 1) % 6;
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