using System.Collections.Generic;
using UnityEngine;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine.Playables;
using UniRx;

namespace HexRPG.Battle.Combat
{
    //! ��ѓ���Combat
    public abstract class AbstractProjectileCombat : AbstractCombatBehaviour
    {
        bool _isAlreadyEmitted = false; // ���ɍU�����������
        bool _isAlreadyFinishCombatAnimation = false;
        List<Vector3> _attackColliderPosCache = new List<Vector3>();

        CancellationTokenSource _cts = null;

        protected override void InternalInit(IAttackComponentCollection attackOwner, IAnimationController animationController, PlayableAsset timeline)
        {
            base.InternalInit(attackOwner, animationController, timeline);

            // AttackCollider�̈ʒu���L���b�V������
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
            //TODO: ����A��ѓ������(collider������)�̏ꍇ�͍l�����Ă��Ȃ�(�����̏ꍇ��collider�ɉ�����CancellationToken���K�v & �_���[�W���ɏ���Collider�����؂���K�v)
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

        // �A�j���[�V�������f/����I����
        protected override void OnFinishCombat()
        {
            _isAlreadyFinishCombatAnimation = true;

            // �����˂̂Ƃ�(�܂蒆�f��)�̂݁A������Timeline��~ -> AttackCollider������ & Combat��finish
            if (_isAlreadyEmitted == false) _director.Stop();

            // �_���[�W�Œ��f���ꂽ�����ˍς�: timeline��stop����܂ő҂� -> timeline stop����AttackCollider���� & OnFinishCombat���s -> Damaged�X�e�[�g�ɑJ�ڍς݂Ȃ̂ňӖ��Ȃ���...
            // �Ō�܂ŃA�j���[�V�����Đ��ς݂̏ꍇ�͂��̂܂�Timeline��stop����܂ő҂� -> Timeline stop����AttackCollider������
        }

        protected async virtual UniTaskVoid OnTimelineStopped()
        {
            FinishAttack();
            _disposables.Clear();

            // AttackCollider�����̈ʒu�ɖ߂�
            for (int i = 0; i < _attackColliders.Count; i++) _attackColliders[i].transform.localPosition = _attackColliderPosCache[i];

            await UniTask.WaitUntil(() => _isAlreadyFinishCombatAnimation);
            _onFinishCombat.OnNext(Unit.Default); // OnFinishCombat�𔭍s����̂�animation����OnFinishCombat�𔭍s���Ă���
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
