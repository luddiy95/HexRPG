using System;
using UniRx;
using UniRx.Triggers;

namespace HexRPG.Battle.Player
{
    using Skill;

    public class PlayerSkillController : AbstractCustomComponentBehaviour, ISkillController, ISkillObservable
    {
        IMemberObservable _memberObservable;
        IAnimatorController _animatorController;
        ITransformController _transformController;
        ISelectSkillObservable _selectSkillObservable;

        CompositeDisposable _disposables = new CompositeDisposable();

        // égÇÌÇ»Ç¢
        ICustomComponentCollection[] ISkillController.SkillList => null;

        ICustomComponentCollection ISkillController.RunningSkill => _runningSkill;
        ICustomComponentCollection _runningSkill;

        IObservable<Unit> ISkillObservable.OnStartSkill => _onStartSkill;
        ISubject<Unit> _onStartSkill = new Subject<Unit>();

        IObservable<Unit> ISkillObservable.OnFinishSkill => _onFinishSkill;
        ISubject<Unit> _onFinishSkill = new Subject<Unit>();

        public override void Register(ICustomComponentCollection owner)
        {
            base.Register(owner);

            owner.RegisterInterface<ISkillController>(this);
            owner.RegisterInterface<ISkillObservable>(this);
        }

        public override void Initialize()
        {
            base.Initialize();

            Owner.QueryInterface(out _memberObservable);
            Owner.QueryInterface(out _animatorController);
            Owner.QueryInterface(out _transformController);
            Owner.QueryInterface(out _selectSkillObservable);
        }

        bool ISkillController.TryStartSkill(int index)
        {
            var curMember = _memberObservable.CurMember.Value;

            if (!curMember.QueryInterface(out ISkillController skillController)) return false;
            if (skillController.TryStartSkill(index))
            {
                void SubscribeSkillAnimationEvent(string tag)
                {
                    _disposables.Clear();

                    var trigger = _animatorController.CurAnimator.Value.GetBehaviour<ObservableStateMachineTrigger>();

                    trigger
                        .OnStateEnterAsObservable()
                        .Where(x => x.StateInfo.IsTag(tag))
                        .Subscribe(_ =>
                        {
                            _onStartSkill.OnNext(Unit.Default);
                            _animatorController.ResetTrigger(tag);
                        })
                        .AddTo(_disposables);

                    trigger
                        .OnStateExitAsObservable()
                        .Where(x => x.StateInfo.IsTag(tag))
                        .Subscribe(_ =>
                        {
                            _transformController.SetRotation(0);
                            skillController.FinishSkill();
                            _onFinishSkill.OnNext(Unit.Default);
                        })
                        .AddTo(_disposables);
                }

                skillController.RunningSkill.QueryInterface(out ISkillSetting skill);

                string param = skill.SkillAnimationParam;
                SubscribeSkillAnimationEvent(param);
                _animatorController.SetTrigger(param);

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
    }
}
