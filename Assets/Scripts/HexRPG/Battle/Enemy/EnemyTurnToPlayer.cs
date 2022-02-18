using UnityEngine;
using Zenject;

namespace HexRPG.Battle.Enemy
{
    public class EnemyTurnToPlayer : ITurnToTarget, IInitializable
    {
        IBattleObservable _battleObservable;
        ITransformController _transformController;
        IMoveSetting _moveSetting;

        float _rotateSpeed = 0;

        public EnemyTurnToPlayer(
            IBattleObservable battleObservable,
            ITransformController transformController,
            IMoveSetting moveSetting)
        {
            _battleObservable = battleObservable;
            _transformController = transformController;
            _moveSetting = moveSetting;
        }

        void IInitializable.Initialize()
        {
            _rotateSpeed = _moveSetting.MoveSpeed;
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
