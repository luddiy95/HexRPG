using System.Collections.Generic;
using UnityEngine.Playables;
using UnityEngine;
using System;
using UniRx;
using UnityEngine.Timeline;

namespace HexRPG.Playable
{
    [Serializable]
    public class AttackEnableBehaviour : PlayableBehaviour
    {
        public IObservable<Unit> OnAttackEnable => _onAttackEnable;
        readonly ISubject<Unit> _onAttackEnable = new Subject<Unit>();

        public IObservable<Unit> OnAttackDisable => _onAttackDisable;
        readonly ISubject<Unit> _onAttackDisable = new Subject<Unit>();

        public int damage;
        public List<Vector2> attackRange;
        public string attackEffectTrack;
        public Vector2 attackEffectOffset; // SkillCenter‚É‘Î‚·‚éOffset

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
