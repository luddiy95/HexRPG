using UnityEngine;
using UnityEditor;
using UniRx;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace HexRPG.Battle.Enemy
{
    using static ActionStateType;

    public class BaseDynamicCombatEnemyActionStateController : AbstractDynamicEnemyActionStateController
    {
        protected override ActionState BuildActionStates()
        {
            NewState(COMBAT)
                .AddEvent(new ActionEventCombat())
                .AddEvent(new ActionEventCancel("finishCombat", IDLE))
                .AddEvent(new ActionEventCancel("damaged", DAMAGED))
                ;

            NewState(ROTATE)
                .AddEvent(new ActionEventPlayMotion(0f))
                .AddEvent(new ActionEventRotate())
                .AddEvent(new ActionEventCancel("idle", IDLE))
                .AddEvent(new ActionEventCancel("combat", COMBAT))
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
                    _rotateAngle = _locomotionController.LookRotate(TargetHex.transform.position, LOOK_ROTATE_SPEED);
                })
                .AddTo(_disposables);

            // Combat
            _actionStateObservable
                .OnStart<ActionEventCombat>()
                .Subscribe(_ =>
                {
                    _combatController.Combat();
                })
                .AddTo(_disposables);
        }

        void Update()
        {
            //TODO: �e�X�g�R�[�h
            if (Input.GetKeyDown(KeyCode.A))
            {
                _actionStateController.Execute(new Command { Id = "combat" });
            }
        }

        protected override ActionStateType AttackableBreakStateType => COMBAT;

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
                case COMBAT:
                    _actionStateController.Execute(new Command { Id = "rotate" });
                    await _locomotionObservable.OnFinishRotate.ToUniTask(useFirstValue: true, cancellationToken: token);

                    // Combat
                    _actionStateController.Execute(new Command { Id = "combat" });
                    await _combatObservable.OnFinishCombat.ToUniTask(useFirstValue: true, cancellationToken: token);
                    _actionStateController.Execute(new Command { Id = "finishCombat" });

                    await UniTask.Delay(1000, cancellationToken: token); // Delay��������������idle->rotate�J�ڒ���idle���荞�݂̉\��������

                    _actionStateController.Execute(new Command { Id = "rotate" });
                    await _locomotionObservable.OnFinishRotate.ToUniTask(useFirstValue: true, cancellationToken: token);
                    _actionStateController.Execute(new Command { Id = "idle" }); break;
            }
        }

#if UNITY_EDITOR

        async UniTaskVoid CombatTest(CancellationToken token)
        {
            _actionStateController.Execute(new Command { Id = "rotate" });
            await _locomotionObservable.OnFinishRotate.ToUniTask(useFirstValue: true, cancellationToken: token);

            // Combat
            _actionStateController.Execute(new Command { Id = "combat" });
            await _combatObservable.OnFinishCombat.ToUniTask(useFirstValue: true, cancellationToken: token);
            _actionStateController.Execute(new Command { Id = "finishCombat" });

            await UniTask.Delay(1000, cancellationToken: token); // Delay��������������idle->rotate�J�ڒ���idle���荞�݂̉\��������

            _actionStateController.Execute(new Command { Id = "rotate" });
            await _locomotionObservable.OnFinishRotate.ToUniTask(useFirstValue: true, cancellationToken: token);
            _actionStateController.Execute(new Command { Id = "idle" });
        }

        public void OnInspectorGUI()
        {
            if (GUILayout.Button("Combat"))
            {
                CombatTest(this.GetCancellationTokenOnDestroy()).Forget();
            }
        }

        [CustomEditor(typeof(BaseDynamicCombatEnemyActionStateController))]
        public class CustomInspector : Editor
        {
            public override void OnInspectorGUI()
            {
                base.OnInspectorGUI();

                ((BaseDynamicCombatEnemyActionStateController)target).OnInspectorGUI();
            }
        }

#endif
    }
}
