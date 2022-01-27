using UnityEngine;
using UniRx;
using UniRx.Triggers;

namespace HexRPG.Battle.Player
{
    using Stage;

    public class PlayerMoveController : AbstractCustomComponentBehaviour, IMoveController
    {
        readonly string MOVE = "Move";

        ITransformController _transformController;
        IDeltaTime _deltaTime;
        IActionStateController _actionStateController;

        [Header("移動できるHexを示すインジケータ")]
        [SerializeField] Transform _moveableIndicatorRoot;

        CompositeDisposable _disposables = new CompositeDisposable();

#nullable enable
        (Vector3 StartPos, Vector3 GoalPos)? _movePos;
#nullable disable
        
        float _speed = 0;

        public override void Register(ICustomComponentCollection owner)
        {
            base.Register(owner);

            owner.RegisterInterface<IMoveController>(this);
        }

        public override void Initialize()
        {
            base.Initialize();

            Owner.QueryInterface(out _transformController);
            Owner.QueryInterface(out _deltaTime);
            Owner.QueryInterface(out _actionStateController);

            // 移動
            if(Owner.QueryInterface(out IUpdateObservable updateObservable))
            {
                updateObservable.OnUpdate((int)UPDATE_ORDER.MOVE)
                    .Subscribe(_ =>
                    {
#nullable enable
                        if(_movePos != null)
                        {
                            var direction = _movePos?.GoalPos - _movePos?.StartPos;
                            if((_transformController.Position - _movePos?.StartPos)?.magnitude >= direction?.magnitude)
                            {
                                _transformController.Position = (Vector3)_movePos?.GoalPos;
                                _movePos = null;

                                // 移動終了処理
                                UpdateMoveableIndicator();
                                _actionStateController.ExecuteTransition(ActionStateType.IDLE);
                            }
                            else
                            {
                                _transformController.Position += (Vector3)(_deltaTime.DeltaTime * _speed * direction / direction?.magnitude);
                            }
                        }
#nullable disable
                    })
                    .AddTo(this);
            }

            if (Owner.QueryInterface(out ISkillObservable skillObservable))
            {
                skillObservable.OnFinishSkill
                    .DelayFrame(1) // LiberateHexが完了してから
                    .Subscribe(_ => UpdateMoveableIndicator())
                    .AddTo(this);
            }

            if(Owner.QueryInterface(out IMemberObservable memberObservable))
            {
                memberObservable.CurMember
                    .Where(member => member != null)
                    .Subscribe(member =>
                    {
                        if(member.QueryInterface(out IMoveSetting moveSetting))
                        {
                            _speed = moveSetting.MoveSpeed;
                        }
                    })
                    .AddTo(this);
            }

            if(Owner.QueryInterface(out IAnimatorController animatorController))
            {
                animatorController.CurAnimator
                    .Where(animator => animator != null)
                    .Subscribe(animator =>
                    {
                        _disposables.Clear();

                        var trigger = animator.GetBehaviour<ObservableStateMachineTrigger>();

                        trigger
                            .OnStateEnterAsObservable()
                            .Where(x => x.StateInfo.IsTag(MOVE))
                            .Subscribe(_ =>
                            {
                                animatorController.SetSpeed(0, 0);
                            })
                            .AddTo(_disposables);

                        trigger
                            .OnStateExitAsObservable()
                            .Where(x => x.StateInfo.IsTag(MOVE))
                            .Subscribe(_ =>
                            {

                            })
                            .AddTo(_disposables);
                    })
                    .AddTo(this);
            }

            if(Owner.QueryInterface(out ITransformController transformController))
            {
                transformController.SetRotation(0);
            }

            UpdateMoveableIndicator();
        }

        void IMoveController.StartMove(Hex destinationHex)
        {
            _movePos = (_transformController.GetLandedHex().transform.position, destinationHex.transform.position);
            _moveableIndicatorRoot.gameObject.SetActive(false);
        }

        void UpdateMoveableIndicator()
        {
            for (int i = 0; i < _moveableIndicatorRoot.childCount; i++)
            {
                Transform indicator = _moveableIndicatorRoot.GetChild(i);
                Hex landedHex = TransformExtensions.GetLandedHex(indicator.position);
                if (landedHex == null) indicator.gameObject.SetActive(false);
                else indicator.gameObject.SetActive(landedHex.IsPlayerHex);
            }

            _moveableIndicatorRoot.gameObject.SetActive(true);
        }
    }
}
