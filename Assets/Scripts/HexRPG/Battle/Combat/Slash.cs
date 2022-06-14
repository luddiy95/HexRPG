using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Timeline;
using UniRx;

namespace HexRPG.Battle.Combat
{
    using Playable;

    public class Slash : AbstractCombatBehaviour
    {
        protected override void InternalExecute()
        {
            foreach (var trackAsset in (_director.playableAsset as TimelineAsset).GetOutputTracks())
            {
                // Attack”»’è
                if (trackAsset is AttackColliderTrack)
                {
                    foreach (var clip in trackAsset.GetClips())
                    {
                        var behaviour = (clip.asset as AttackColliderAsset).behaviour;
                        behaviour.OnAttackEnable
                            .Subscribe(_ =>
                            {
                                _attackColliders.ForEach(collider => 
                                { 
                                    if(collider.TryGetComponent(out Rigidbody rigidbody)) rigidbody.velocity = collider.transform.forward * 5f;
                                });
                            })
                            .AddTo(_disposables);
                    }
                }
            }
            base.InternalExecute();
        }

        protected override void OnAttackHit()
        {
            OnAttackDisable();
            base.OnAttackHit();
        }

        protected override void OnFinishCombat()
        {
            _attackColliders.ForEach(collider =>
            {
                if (collider.TryGetComponent(out Rigidbody rigidbody)) rigidbody.velocity = Vector3.zero;
                collider.transform.position = Vector3.up * collider.transform.position.y;
            });

            base.OnFinishCombat();
        }
    }
}
