using UnityEngine;
using UnityEngine.Playables;
using System;
using UniRx;

namespace HexRPG.Playable
{
    [Serializable]
    public class VelocityBehaviour : PlayableBehaviour
    {
        public IObservable<Vector3> Velocity => _velocity;
        readonly ISubject<Vector3> _velocity = new Subject<Vector3>();

        public Vector3 direction;
        public float speed;

        public override void PrepareFrame(UnityEngine.Playables.Playable playable, FrameData info)
        {
            base.PrepareFrame(playable, info);

            var velocityPlayable = (ScriptPlayable<VelocityBehaviour>)playable;
            var behaviour = velocityPlayable.GetBehaviour();
            _velocity.OnNext(behaviour.direction.normalized * behaviour.speed);
        }
    }
}
