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
        IAnimatorController _animatorController;
        IMemberObservable _memberObservable;
        ISelectSkillObservable _selectSkillObservable;
        IStageController _stageController;

        CompositeDisposable _animationDisposables = new CompositeDisposable();

        // égÇÌÇ»Ç¢
        ISkillComponentCollection[] ISkillController.SkillList => null;

        ISkillComponentCollection ISkillController.RunningSkill => null;

        IObservable<Unit> ISkillObservable.OnStartSkill => _onStartSkill;
        ISubject<Unit> _onStartSkill = new Subject<Unit>();

        IObservable<Unit> ISkillObservable.OnFinishSkill => _onFinishSkill;
        ISubject<Unit> _onFinishSkill = new Subject<Unit>();

        public PlayerSkillExecuter(
            ITransformController transformController,
            IAnimatorController animatorController,
            IMemberObservable memberObservable,
            ISelectSkillObservable selectSkillObservable,
            IStageController stageController
        )
        {
            _transformController = transformController;
            _animatorController = animatorController;
            _memberObservable = memberObservable;
            _selectSkillObservable = selectSkillObservable;
            _stageController = stageController;
        }

        bool ISkillController.TryStartSkill(int index, List<Hex> attackRange)
        {
            var memberOwner = _memberObservable.CurMember.Value;

            var skillController = memberOwner.SkillController;
            if (skillController.TryStartSkill(index, _selectSkillObservable.CurAttackIndicateHexList))
            {
                var skillSetting = skillController.RunningSkill.SkillSetting;

                Action enterCallback = () =>
                {
                    _onStartSkill.OnNext(Unit.Default);
                };
                Action exitCallback = () =>
                {
                    _transformController.SetRotation(0);
                    skillController.FinishSkill();
                    _stageController.Liberate(_selectSkillObservable.CurAttackIndicateHexList, true);
                    _onFinishSkill.OnNext(Unit.Default);
                };
                string param = skillSetting.SkillAnimationParam;
                _animatorController.SetTriggerWithCallback(enterCallback, exitCallback, param);

                _transformController.SetRotation(_selectSkillObservable.DuplicateSelectedCount * 60);

                return true;
            }
            else
            {
                //TODO: Skillé¿çsÇ≈Ç´Ç»Ç©Ç¡ÇΩèÍçá

                return false;
            }
        }

        void ISkillController.FinishSkill()
        {

        }

        void ISkillController.StartSkillEffect()
        {

        }

        void IDisposable.Dispose()
        {
            _animationDisposables.Dispose();
        }
    }
}
