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
        bool _isAlreadyEmitted = false; // 既に攻撃を放ったか
        bool _isAlreadyFinishCombatAnimation = false;
        List<Vector3> _attackColliderPosCache = new List<Vector3>();

        CancellationTokenSource _cts = null;

        protected override void InternalInit(IAttackComponentCollection attackOwner, IAnimationController animationController, PlayableAsset timeline)
        {
            base.InternalInit(attackOwner, animationController, timeline);

            // AttackColliderの位置をキャッシュする
            foreach (var collider in _attackColliders) _attackColliderPosCache.Add(collider.transform.localPosition);

            _director.stopped += (_ => OnTimelineStopped().Forget());
        }

        protected override void InternalExecute()
        {
            base.InternalExecute();
            _isAlreadyEmitted = false;
            _isAlreadyFinishCombatAnimation = false;
        }

        protected override void OnAttackEnable(int damage, Vector3 colliderVelocity)
        {
            _isAlreadyEmitted = false;
            _cts = new CancellationTokenSource(); 
            //TODO: 現状、飛び道具が複数(colliderが複数)の場合は考慮していない(複数の場合はcolliderに応じたCancellationTokenが必要 & ダメージ時に消すColliderを検証する必要)
            foreach(var collider in _attackColliders) Emit(_cts.Token, collider, colliderVelocity).Forget();
            base.OnAttackEnable(damage, colliderVelocity);
        }

        protected abstract UniTaskVoid Emit(CancellationToken token, AttackCollider collider, Vector3 colliderVelocity);

        public virtual void OnEmitComplete()
        {
            _isAlreadyEmitted = true;
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
            _isAlreadyFinishCombatAnimation = true;

            // 未発射のとき(つまり中断時)のみ、即座にTimeline停止 -> AttackColliderを消す & Combatをfinish
            if (_isAlreadyEmitted == false) _director.Stop();

            // ダメージで中断されたが発射済み: timelineがstopするまで待つ -> timeline stop時にAttackCollider消す & OnFinishCombat発行 -> Damagedステートに遷移済みなので意味ないが...
            // 最後までアニメーション再生済みの場合はそのままTimelineがstopするまで待つ -> Timeline stop時にAttackColliderを消す
        }

        protected async virtual UniTaskVoid OnTimelineStopped()
        {
            FinishAttack();
            _disposables.Clear();

            // AttackColliderを元の位置に戻す
            for (int i = 0; i < _attackColliders.Count; i++) _attackColliders[i].transform.localPosition = _attackColliderPosCache[i];

            await UniTask.WaitUntil(() => _isAlreadyFinishCombatAnimation);
            _onFinishCombat.OnNext(Unit.Default); // OnFinishCombatを発行するのはanimation側がOnFinishCombatを発行してから
        }

        protected override void InternalDispose()
        {
            base.InternalDispose();
            TokenCancel();
        }

        protected void TokenCancel()
        {
            _cts?.Cancel();
            _cts?.Dispose();
            _cts = null;
        }
    }
}
