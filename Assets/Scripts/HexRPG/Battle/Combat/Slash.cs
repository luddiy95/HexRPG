using UnityEngine;
using UnityEngine.VFX;
using Cysharp.Threading.Tasks;
using System.Threading;

namespace HexRPG.Battle.Combat
{
    public class Slash : AbstractProjectileCombat
    {
        protected override async UniTaskVoid Emit(CancellationToken token, AttackCollider collider)
        {
            if (collider.TryGetComponent(out Rigidbody rigidbody)) rigidbody.velocity = collider.transform.forward * 8f;

            await UniTask.Delay(250, cancellationToken: token);

            var effect = collider.transform.GetChild(0).GetComponent<VisualEffect>();
            effect.playRate = 0;

            TokenCancel();
        }

        protected override void OnTimelineStopped()
        {
            base.OnTimelineStopped();
            _attackColliders.ForEach(collider =>
            {
                if (collider.TryGetComponent(out Rigidbody rigidbody)) rigidbody.velocity = Vector3.zero;

                var effect = collider.transform.GetChild(0).GetComponent<VisualEffect>();
                effect.playRate = 1;
            });
        }
    }
}
