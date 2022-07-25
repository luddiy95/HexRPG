using UnityEngine;
using UnityEditor;
using UniRx;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace HexRPG.Battle.Enemy
{
    using static ActionStateType;

    public class BaseDynamicSkillEnemyActionStateController : AbstractDynamicEnemyActionStateController
    {
        protected override ActionState BuildActionStates()
        {
            NewState(SKILL)
                .AddEvent(new ActionEventSkill())
                .AddEvent(new ActionEventCancel("finishSkill", IDLE))
                .AddEvent(new ActionEventCancel("damaged", DAMAGED))
                ;

            NewState(ROTATE)
                .AddEvent(new ActionEventPlayMotion(0f))
                .AddEvent(new ActionEventRotate())
                .AddEvent(new ActionEventCancel("idle", IDLE))
                .AddEvent(new ActionEventCancel("skill", SKILL))
                .AddEvent(new ActionEventCancel("damaged", DAMAGED))
                ;

            return base.BuildActionStates();
        }

        protected override void SetUpControl()
        {
            base.SetUpControl();

            // Rotate
            _actionStateObservable
                .OnStart<ActionEventRotate>()
                .Subscribe(_ =>
                {
                    _rotateAngle = _locomotionController.LookRotate60(TargetHex.transform.position, LOOK_ROTATE_SPEED);
                })
                .AddTo(_disposables);

            // �X�L�����s
            _actionStateObservable
                .OnStart<ActionEventSkill>()
                .Subscribe(_ =>
                {
                    _skillController.StartSkill(0); //TODO: 0�͉�(����Skill����Enemy�����Ă��ǂ�)
                })
                .AddTo(_disposables);
        }

        void Update()
        {
            //TODO: �e�X�g�R�[�h
            if (Input.GetKeyDown(KeyCode.A))
            {
                _actionStateController.Execute(new Command { Id = "skill" });
            }
        }

        protected override ActionStateType AttackableBreakStateType => SKILL;

        protected override async UniTask BreakState(ActionStateType breakStateType, CancellationToken token)
        {
            switch (breakStateType)
            {
                case IDLE:
                    //! �ǂ��ɂ��ړ��ł���landedHex�ɋ������邵���Ȃ��ꍇ�́Aidle -> rotate -> idle�J�ڂ��p�ɂɍs����̂ŁA���̑J�ڂł���a���̂Ȃ����[�V�����ɂ���K�v������
                    _actionStateController.Execute(new Command { Id = "rotate" });
                    await _locomotionObservable.OnFinishRotate.ToUniTask(useFirstValue: true, cancellationToken: token);
                    _actionStateController.Execute(new Command { Id = "idle" });
                    break;
                case SKILL:
                    _actionStateController.Execute(new Command { Id = "rotate" });
                    await _locomotionObservable.OnFinishRotate.ToUniTask(useFirstValue: true, cancellationToken: token);

                    // Skill
                    _actionStateController.Execute(new Command { Id = "skill" });
                    await _skillObservable.OnFinishSkill.ToUniTask(useFirstValue: true, cancellationToken: token);
                    _actionStateController.Execute(new Command { Id = "finishSkill" });

                    await UniTask.Delay(1000, cancellationToken: token); // Delay��������������idle->rotate�J�ڒ���idle���荞�݂̉\��������

                    _actionStateController.Execute(new Command { Id = "rotate" });
                    await _locomotionObservable.OnFinishRotate.ToUniTask(useFirstValue: true, cancellationToken: token);
                    _actionStateController.Execute(new Command { Id = "idle" }); break;
            }
        }
    }
}
