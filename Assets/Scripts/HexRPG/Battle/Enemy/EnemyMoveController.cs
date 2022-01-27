using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UniRx;

namespace HexRPG.Battle.Enemy
{
    using Stage;

    public class EnemyMoveController : AbstractCustomComponentBehaviour, IMoveController
    {
        ITurnToTarget _turnToTarget;

        public override void Register(ICustomComponentCollection owner)
        {
            base.Register(owner);

            owner.RegisterInterface<IMoveController>(this);
        }

        public override void Initialize()
        {
            base.Initialize();

            Owner.QueryInterface(out _turnToTarget);

            if(Owner.QueryInterface(out IBattleObservable battleObservable))
            {
                battleObservable.OnBattleStart
                    .First()
                    .Subscribe(_ => _turnToTarget.TurnToTarget())
                    .AddTo(this);
            }
        }

        void IMoveController.StartMove(Hex destinationHex)
        {

        }
    }
}
