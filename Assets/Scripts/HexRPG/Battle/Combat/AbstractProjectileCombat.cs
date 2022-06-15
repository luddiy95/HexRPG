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
        bool _alreadyEmitted = false; // ���ɍU�����������
        List<Vector3> _attackColliderPosCache = new List<Vector3>();

        CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();

        protected override void InternalInit(IAttackComponentCollection attackOwner, IAnimationController animationController, PlayableAsset timeline)
        {
            base.InternalInit(attackOwner, animationController, timeline);

            // AttackCollider�̈ʒu���L���b�V������
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
            //! ����A��ѓ������(collider������)�̏ꍇ�͍l�����Ă��Ȃ�(�����̏ꍇ��collider�ɉ�����CancellationToken���K�v & �_���[�W���ɏ���Collider�����؂���K�v)
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

        // �A�j���[�V�������f/����I����
        protected override void OnFinishCombat()
        {
            // �����˂̂Ƃ�(�܂蒆�f��)�̂݁A������Timeline��~ -> AttackCollider������ & Combat��finish
            if (_alreadyEmitted == false) _director.Stop();

            // �_���[�W�Œ��f���ꂽ�����ˍς�: timeline��stop����܂ő҂� -> timeline stop����AttackCollider���� & OnFinishCombat���s -> Damaged�X�e�[�g�ɑJ�ڍς݂Ȃ̂ňӖ��Ȃ���...
            // �Ō�܂ŃA�j���[�V�����Đ��ς݂̏ꍇ�͂��̂܂�Timeline��stop����܂ő҂� -> Timeline stop����AttackCollider������
        }

        protected virtual void OnTimelineStopped()
        {
            FinishAttack();
            _disposables.Clear();
            _onFinishCombat.OnNext(Unit.Default);

            // AttackCollider�����̈ʒu�ɖ߂�
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
