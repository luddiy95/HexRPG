using UnityEngine;
using UniRx;
using System;
using Zenject;

namespace HexRPG.Battle.Player
{
    using Stage;

    public class PlayerMover : IMover, IInitializable, IDisposable
    {
        ITransformController _transformController;
        IMoveableIndicator _moveableIndicator;
        IUpdateObservable _updateObservable;
        IDeltaTime _deltaTime;
        IActionStateController _actionStateController;
        ISkillObservable _skillObservable;
        IMemberObservable _memberObservable;

        CompositeDisposable _disposables = new CompositeDisposable();

#nullable enable
        (Vector3 StartPos, Vector3 GoalPos)? _movePos;
#nullable disable

        float _speed = 0;

        public PlayerMover(
            ITransformController transformController,
            IMoveableIndicator moveableIndicator,
            IUpdateObservable updateObservable,
            IDeltaTime deltaTime,
            IActionStateController actionStateController,
            ISkillObservable skillObservable,
            IMemberObservable memberObservable
        )
        {
            _transformController = transformController;
            _moveableIndicator = moveableIndicator;
            _updateObservable = updateObservable;
            _deltaTime = deltaTime;
            _actionStateController = actionStateController;
            _skillObservable = skillObservable;
            _memberObservable = memberObservable;
        }

        void IInitializable.Initialize()
        {
            _updateObservable.OnUpdate((int)UPDATE_ORDER.MOVE)
                .Subscribe(_ =>
                {
#nullable enable
                    if (_movePos != null)
                    {
                        var direction = _movePos?.GoalPos - _movePos?.StartPos;
                        if ((_transformController.Position - _movePos?.StartPos)?.magnitude >= direction?.magnitude)
                        {
                            _transformController.Position = (Vector3)_movePos?.GoalPos;
                            _movePos = null;

                            // ˆÚ“®I—¹ˆ—
                            _moveableIndicator.UpdateIndicator();
                            _actionStateController.ExecuteTransition(ActionStateType.IDLE);
                        }
                        else
                        {
                            _transformController.Position += (Vector3)(_deltaTime.DeltaTime * _speed * direction / direction?.magnitude);
                        }
                    }
#nullable disable
                })
                .AddTo(_disposables);

            _skillObservable.OnFinishSkill
                .DelayFrame(1) // LiberateHex‚ªŠ®—¹‚µ‚Ä‚©‚ç
                .Subscribe(_ => _moveableIndicator.UpdateIndicator())
                .AddTo(_disposables);

            _memberObservable.CurMember
                .Skip(1)
                .Subscribe(memberOwner =>
                {
                    _speed = memberOwner.MoveSetting.MoveSpeed;
                })
                .AddTo(_disposables);

            _transformController.SetRotation(0);

            _moveableIndicator.UpdateIndicator();
        }

        void IMover.StartMove(Hex destinationHex)
        {
            _movePos = (_transformController.GetLandedHex().transform.position, destinationHex.transform.position);
            _moveableIndicator.SwitchShow(false);
        }

        void IDisposable.Dispose()
        {
            _disposables.Dispose();
        }
    }
}
