using UnityEngine;

namespace HexRPG.Battle.Enemy
{
    public class EnemyTurnToPlayer : AbstractCustomComponentBehaviour, ITurnToTarget
    {
        IBattleObservable _battleObservable;
        ITransformController _transformController;

        float _rotateSpeed = 0;

        public override void Register(ICustomComponentCollection owner)
        {
            base.Register(owner);

            owner.RegisterInterface<ITurnToTarget>(this);
        }

        public override void Initialize()
        {
            base.Initialize();

            Owner.QueryInterface(out _battleObservable);
            Owner.QueryInterface(out _transformController);
            if (Owner.QueryInterface(out IMoveSetting moveSetting)) _rotateSpeed = moveSetting.RotateSpeed;
        }

        void ITurnToTarget.TurnToTarget()
        {
            var playerLandedHex = _battleObservable.PlayerLandedHex;
            if (playerLandedHex == null) return;

            var landedHex = _transformController.GetLandedHex();
            var relativePos = playerLandedHex.transform.position - landedHex.transform.position;
            relativePos.y = 0;

            _transformController.Rotation = Quaternion.LookRotation(relativePos);
        }
    }
}
