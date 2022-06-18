using UnityEngine;
using UnityEngine.Playables;
using System;
using UniRx;

namespace HexRPG.Playable
{
    [Serializable]
    public class AttackColliderBehaviour : PlayableBehaviour
    {
        public IObservable<Unit> OnAttackEnable => _onAttackEnable;
        readonly ISubject<Unit> _onAttackEnable = new Subject<Unit>();

        public IObservable<Unit> OnAttackDisable => _onAttackDisable;
        readonly ISubject<Unit> _onAttackDisable = new Subject<Unit>();

        public Vector3 direction;
        public float speed;

        public Vector3 Velocity => direction.normalized * speed;

        public override void OnBehaviourPlay(UnityEngine.Playables.Playable playable, FrameData info)
        {
            base.OnBehaviourPlay(playable, info);

            _onAttackEnable.OnNext(Unit.Default);
        }

        public override void OnBehaviourPause(UnityEngine.Playables.Playable playable, FrameData info)
        {
            base.OnBehaviourPause(playable, info);

            if (!playable.IsClipEnded(info)) return;

            _onAttackDisable.OnNext(Unit.Default);
        }
    }
}
