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

            // スキル実行
            _actionStateObservable
                .OnStart<ActionEventSkill>()
                .Subscribe(_ =>
                {
                    _skillController.StartSkill(0); //TODO: 0は仮(複数Skill持つEnemyがいても良い)
                })
                .AddTo(_disposables);
        }

        void Update()
        {
            //TODO: テストコード
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
                    //! どこにも移動できずlandedHexに居続けるしかない場合は、idle -> rotate -> idle遷移が頻繁に行われるので、その遷移でも違和感のないモーションにする必要がある
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

                    await UniTask.Delay(1000, cancellationToken: token); // Delayが小さかったらidle->rotate遷移中にidle割り込みの可能性がある

                    _actionStateController.Execute(new Command { Id = "rotate" });
                    await _locomotionObservable.OnFinishRotate.ToUniTask(useFirstValue: true, cancellationToken: token);
                    _actionStateController.Execute(new Command { Id = "idle" }); break;
            }
        }
    }
}
