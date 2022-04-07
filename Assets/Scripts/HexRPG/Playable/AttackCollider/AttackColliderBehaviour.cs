using UnityEngine;
using UnityEngine.Playables;
using System;
using UniRx;

namespace HexRPG.Playable
{
    public class AttackColliderBehaviour : PlayableBehaviour
    {
        public IObservable<Unit> OnAttackEnable => _onAttackEnable;
        readonly ISubject<Unit> _onAttackEnable = new Subject<Unit>();

        public IObservable<Unit> OnAttackDisable => _onAttackDisable;
        readonly ISubject<Unit> _onAttackDisable = new Subject<Unit>();

        bool _isFirstFrame = true;
        public Collider Collider { get; private set; }

        public override void ProcessFrame(UnityEngine.Playables.Playable playable, FrameData info, object playerData)
        {
            base.ProcessFrame(playable, info, playerData);

            if (_isFirstFrame)
            {
                _isFirstFrame = false;

                Collider = playerData as Collider;

                _onAttackEnable.OnNext(Unit.Default);
                //Collider.gameObject.SetActive(true);
            }
        }

        public override void OnBehaviourPause(UnityEngine.Playables.Playable playable, FrameData info)
        {
            base.OnBehaviourPause(playable, info);

            if (!playable.IsClipEnded(info)) return;

            //Collider.gameObject.SetActive(false);
            _onAttackDisable.OnNext(Unit.Default);
        }
    }
}
