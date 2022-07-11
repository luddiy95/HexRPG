using UnityEngine;
using Zenject;
using UniRx;

namespace HexRPG.Battle.Player
{
    public class PlayerLocomotionBehaviour : AbstractLocomotionBehaviour
    {
        IBattleObservable _battleObservable;
        IMemberObservable _memberObservable;

        [Inject]
        public void Construct(
            IBattleObservable battleObservable,
            ITransformController transformController,
            IMemberObservable memberObservable
        )
        {
            _battleObservable = battleObservable;
            _transformController = transformController;
            _memberObservable = memberObservable;
        }

        protected override void Initialize()
        {
            base.Initialize();

            _memberObservable.CurMember
                .Skip(1)
                .Subscribe(member =>
                {
                    _speed = member.MoveSetting.MoveSpeed;
                    _colliderRadius = member.ColliderController.Collider.radius;
                })
                .AddTo(this);
        }

        protected override void InternalSetSpeed(Vector3 direction, float speed)
        {
            // directionÇÃêÊÇ…êiÇﬂÇÈÇ©Ç«Ç§Ç©
            var worldDirection = (Quaternion.AngleAxis(_battleObservable.CameraTransposer.m_Heading.m_Bias, Vector3.up) * direction).normalized;
            var directionHex = TransformExtensions.GetLandedHex(_transformController.Position + worldDirection * _colliderRadius);

            if (directionHex != null && directionHex.IsPlayerHex) Rigidbody.velocity = worldDirection * (float)speed;
            else Rigidbody.velocity = Vector3.zero;
        }
    }
}
