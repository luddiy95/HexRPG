using System.Collections.Generic;
using UnityEngine;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine.Playables;
using UniRx;

namespace HexRPG.Battle.Combat
{
    //! 飛び道具Combat
    public abstract class AbstractProjectileCombat : AbstractCombatBehaviour
    {
        bool _alreadyEmitted = false; // 既に攻撃を放ったか
        List<Vector3> _attackColliderPosCache = new List<Vector3>();

        CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();

        protected override void InternalInit(IAttackComponentCollection attackOwner, IAnimationController animationController, PlayableAsset timeline)
        {
            base.InternalInit(attackOwner, animationController, timeline);

            // AttackColliderの位置をキャッシュする
            _attackColliders.ForEach(collider => _attackColliderPosCache.Add(collider.transform.localPosition));

            _director.stopped += (_ => OnTimelineStopped());
        }

        protected override void InternalExecute()
        {
            base.InternalExecute();
            _alreadyEmitted = false;
        }

        protected override void OnAttackEnable()
        {
            _alreadyEmitted = false;
            _cancellationTokenSource = new CancellationTokenSource(); 
            //! 現状、飛び道具が複数(colliderが複数)の場合は考慮していない(複数の場合はcolliderに応じたCancellationTokenが必要 & ダメージ時に消すColliderを検証する必要)
            _attackColliders.ForEach(collider =>
            {
                Emit(_cancellationTokenSource.Token, collider).Forget();
            });
            base.OnAttackEnable();
        }

        protected abstract UniTaskVoid Emit(CancellationToken token, AttackCollider collider);

        public virtual void OnEmitComplete()
        {
            _alreadyEmitted = true;
        }

        protected override void OnAttackHit()
        {
            FinishAttack();
            base.OnAttackHit();
        }

        protected override void FinishAttack()
        {
            TokenCancel();

            base.FinishAttack();
        }

        // アニメーション中断/正常終了時
        protected override void OnFinishCombat()
        {
            // 未発射のとき(つまり中断時)のみ、即座にTimeline停止 -> AttackColliderを消す & Combatをfinish
            if (_alreadyEmitted == false) _director.Stop();

            // ダメージで中断されたが発射済み: timelineがstopするまで待つ -> timeline stop時にAttackCollider消す & OnFinishCombat発行 -> Damagedステートに遷移済みなので意味ないが...
            // 最後までアニメーション再生済みの場合はそのままTimelineがstopするまで待つ -> Timeline stop時にAttackColliderを消す
        }

        protected virtual void OnTimelineStopped()
        {
            FinishAttack();
            _disposables.Clear();
            _onFinishCombat.OnNext(Unit.Default);

            // AttackColliderを元の位置に戻す
            for (int i = 0; i < _attackColliders.Count; i++) _attackColliders[i].transform.localPosition = _attackColliderPosCache[i];
        }

        protected override void OnDisposed()
        {
            base.OnDisposed();
            TokenCancel();
        }

        protected void TokenCancel()
        {
            _cancellationTokenSource?.Cancel();
            _cancellationTokenSource?.Dispose();
            _cancellationTokenSource = null;
        }
    }
}
