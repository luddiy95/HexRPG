using System.Collections.Generic;
using System;
using UniRx;

namespace HexRPG.Battle.Player
{
    using Stage;
    using Skill;

    public class PlayerSkillExecuter : ISkillController, ISkillObservable, IDisposable
    {
        ITransformController _transformController;
        IMemberObservable _memberObservable;
        ISelectSkillObservable _selectSkillObservable;
        IStageController _stageController;

        CompositeDisposable _disposables = new CompositeDisposable();

        // �g��Ȃ�
        ISkillComponentCollection[] ISkillController.SkillList => null;

        IObservable<Unit> ISkillObservable.OnStartSkill => _onStartSkill;
        ISubject<Unit> _onStartSkill = new Subject<Unit>();

        IObservable<Unit> ISkillObservable.OnFinishSkill => _onFinishSkill;
        ISubject<Unit> _onFinishSkill = new Subject<Unit>();

        public PlayerSkillExecuter(
            ITransformController transformController,
            IMemberObservable memberObservable,
            ISelectSkillObservable selectSkillObservable,
            IStageController stageController
        )
        {
            _transformController = transformController;
            _memberObservable = memberObservable;
            _selectSkillObservable = selectSkillObservable;
            _stageController = stageController;
        }

        ISkillComponentCollection ISkillController.StartSkill(int index, List<Hex> skillRange)
        {
            _disposables.Clear();
            var skillController = _memberObservable.CurMember.Value.SkillController;

            _transformController.RotationAngle = _selectSkillObservable.SelectedSkillRotation - _transformController.DefaultRotation;
            var runningSkill = skillController.StartSkill(index, _selectSkillObservable.CurAttackIndicateHexList);

            var skillObservable = runningSkill.SkillObservable;
            skillObservable.OnStartSkill.Subscribe(_ => _onStartSkill.OnNext(Unit.Default)).AddTo(_disposables);
            skillObservable.OnFinishSkill.Subscribe(_ =>
            {
                _transformController.RotationAngle = 0;
                _stageController.Liberate(_selectSkillObservable.CurAttackIndicateHexList, true);
                _onFinishSkill.OnNext(Unit.Default);
            }).AddTo(_disposables);

            return runningSkill;
        }

        void IDisposable.Dispose()
        {
            _disposables.Dispose();
        }
    }
}
