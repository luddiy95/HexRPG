using UnityEngine;
using UnityEngine.VFX;
using Cysharp.Threading.Tasks;
using System.Threading;

namespace HexRPG.Battle.Combat
{
    public class Slash : AbstractProjectileCombat
    {
        protected override void InternalExecute()
        {
            base.InternalExecute();
            foreach(var collider in _attackColliders)
            {
                if (collider.TryGetComponent(out Rigidbody rigidbody)) rigidbody.velocity = Vector3.zero;

                var effect = collider.transform.GetChild(0).GetComponent<VisualEffect>();
                effect.playRate = 1;
            }
        }

        protected override async UniTaskVoid Emit(CancellationToken token, AttackCollider collider, Vector3 colliderVelocity)
        {
            if (collider.TryGetComponent(out Rigidbody rigidbody)) rigidbody.velocity = Quaternion.LookRotation(collider.transform.forward) * colliderVelocity;

            await UniTask.Delay(250, cancellationToken: token);

            var effect = collider.transform.GetChild(0).GetComponent<VisualEffect>();
            effect.playRate = 0;

            TokenCancel();
        }
    }
}