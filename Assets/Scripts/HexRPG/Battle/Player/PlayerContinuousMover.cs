using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using UniRx;
using Zenject;

namespace HexRPG.Battle.Player
{
    public class PlayerContinuousMover : IMoveController, IInitializable, IDisposable
    {
        IUpdateObservable _updateObservable;

        CompositeDisposable _disposables = new CompositeDisposable();
        bool canMove = false;

        public PlayerContinuousMover(IUpdateObservable updateObservable)
        {
            _updateObservable = updateObservable;
        }

        void IInitializable.Initialize()
        {
            _updateObservable.OnUpdate((int)UPDATE_ORDER.INPUT)
                .Subscribe(_ =>
                {
                    if (canMove)
                    {

                    }
                })
                .AddTo(_disposables);
        }

        void IMoveController.StartMove(Stage.Hex destination)
        {
            canMove = true;
        }

        void IDisposable.Dispose()
        {
            _disposables.Dispose();
        }
    }
}
