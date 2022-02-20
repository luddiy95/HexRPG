using UnityEngine;
using Zenject;
using UniRx;
using System;

namespace HexRPG.Battle.Enemy
{
    public class EnemyTurnToPlayer : ITurnToTarget, IInitializable, IDisposable
    {
        IBattleObservable _battleObservable;
        ITransformController _transformController;
        IMoveSetting _moveSetting;

        float _rotateSpeed = 0;

        CompositeDisposable _disposables = new CompositeDisposable();

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

            _battleObservable.OnBattleStart
                .Subscribe(_ => (this as ITurnToTarget).TurnToTarget())
                .AddTo(_disposables);
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

        void IDisposable.Dispose()
        {
            _disposables.Dispose();
        }
    }
}
