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

        // égÇÌÇ»Ç¢
        ISkillComponentCollection[] ISkillController.SkillList => null;
        ISkillComponentCollection ISkillController.RunningSkill => null;

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

        bool ISkillController.TryStartSkill(int index)
        {
            var memberOwner = _memberObservable.CurMember.Value;
            var skillController = memberOwner.SkillController;

            if (skillController.TryStartSkill(index))
            {
                _disposables.Clear();
                var skillObservable = skillController.RunningSkill.SkillObservable;
                skillObservable.OnStartSkill.Subscribe(_ => _onStartSkill.OnNext(Unit.Default)).AddTo(_disposables);
                skillObservable.OnFinishSkill.Subscribe(_ =>
                {
                    _transformController.SetRotation(0);
                    _stageController.Liberate(_selectSkillObservable.CurAttackIndicateHexList, true);
                    _onFinishSkill.OnNext(Unit.Default);
                }).AddTo(_disposables);

                _transformController.SetRotation(_selectSkillObservable.DuplicateSelectedCount * 60);

                skillController.StartSkill(_selectSkillObservable.CurAttackIndicateHexList);

                return true;
            }
            else
            {
                //TODO: Skillé¿çsÇ≈Ç´Ç»Ç©Ç¡ÇΩèÍçá
                _disposables.Clear();

                return false;
            }
        }

        void ISkillController.StartSkill(List<Hex> attackRange)
        {

        }

        void IDisposable.Dispose()
        {
            _disposables.Dispose();
        }
    }
}
