using UnityEngine;
using System;
using UniRx;

namespace HexRPG.Battle.Player
{
    using Stage;
    using Battle.Skill;

    public class PlayerSkillExecuter : ISkillController, ISkillObservable, IDisposable
    {
        ITransformController _transformController;
        IMemberObservable _memberObservable;
        ISelectSkillObservable _selectSkillObservable;
        IStageController _stageController;

        CompositeDisposable _disposables = new CompositeDisposable();

        IReadOnlyReactiveProperty<Hex[]> ISkillObservable.OnSkillAttack => null;

        IObservable<Unit> ISkillObservable.OnFinishSkill => _onFinishSkill;
        readonly ISubject<Unit> _onFinishSkill = new Subject<Unit>();

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

        ISkillComponentCollection ISkillController.StartSkill(int index, Hex landedHex, int skillRotation)
        {
            _transformController.RotationAngle = _selectSkillObservable.SelectedSkillRotation - _transformController.DefaultRotation;

            var runningSkill = 
                _memberObservable.CurMember.Value.SkillController.StartSkill(
                    index, 
                    _transformController.GetLandedHex(), 
                    _selectSkillObservable.SelectedSkillRotation
                );
            _disposables.Clear();
            runningSkill.SkillObservable.OnSkillAttack
                .Subscribe(attackRange =>
                {
                    //TODO: �U�����e�����skillRange���ɐ����Ă���G�����邩�ǂ���->���Ȃ����Liberate
                    //TODO: �G�̐���������܂����肵�Ă��Ȃ����߁A�Ƃ肠�����G�̗L��/�����ɂ�����炸Liberate
                    _stageController.Liberate(attackRange, true);
                })
                .AddTo(_disposables);
            runningSkill.SkillObservable.OnFinishSkill
                .Subscribe(_ =>
                {
                    _transformController.RotationAngle = 0;
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
