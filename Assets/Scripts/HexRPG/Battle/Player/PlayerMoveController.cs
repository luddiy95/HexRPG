using UnityEngine;
using UniRx;
using UniRx.Triggers;
using DG.Tweening;

namespace HexRPG.Battle.Player
{
    using Stage;

    public interface IMoveSetting : IFeature
    {
        float Speed { get; }
    }

    public class PlayerMoveController : AbstractCustomComponentBehaviour, IMoveController
    {
        readonly string MOVE = "Move";

        IActionStateController _actionStateController;

        [Header("移動できるHexを示すインジケータ")]
        [SerializeField] Transform _moveableIndicatorRoot;

        CompositeDisposable _disposables = new CompositeDisposable();

        float _moveTime;

        public override void Register(ICustomComponentCollection owner)
        {
            base.Register(owner);

            owner.RegisterInterface<IMoveController>(this);
        }

        public override void Initialize()
        {
            base.Initialize();

            Owner.QueryInterface(out _actionStateController);

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
                            _moveTime = 1f / moveSetting.Speed;
                        }
                    })
                    .AddTo(this);
            }

            if(Owner.QueryInterface(out IAnimatorController animatorController))
            {
                animatorController.CurAnimator
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
            transform
                .DOMove(destinationHex.transform.position, _moveTime)
                .onComplete = () =>
                {
                    UpdateMoveableIndicator();
                    _actionStateController.ExecuteTransition(ActionStateType.IDLE);
                };

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
