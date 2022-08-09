using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;
using System;
using UniRx;
using Zenject;

namespace HexRPG.Battle.Combat
{
    using Playable;

    public abstract class AbstractCombatBehaviour : MonoBehaviour, ICombat, ICombatObservable, ICombatAttack, IComboInputEnable, IDisposable
    {
        IAudioController _audioController;
        ICombatSetting _combatSetting;

        protected IAttackComponentCollection _attackOwner;
        IAnimationController _animationController;

        [SerializeField] protected PlayableDirector _director;

        IObservable<Unit> ICombatObservable.OnFinishCombat => _onFinishCombat;
        protected readonly ISubject<Unit> _onFinishCombat = new Subject<Unit>();

        PlayableAsset ICombat.PlayableAsset => _director.playableAsset;

        Transform ICombat.AttackColliderRoot
        {
            set
            {
                _attackColliders.ForEach(attackCollider =>
                {
                    attackCollider.transform.SetParent(value);
                    attackCollider.transform.localPosition = Vector3.zero;
                    attackCollider.transform.localRotation = Quaternion.identity;
                });
            }
        }

        protected List<AttackCollider> _attackColliders = new List<AttackCollider>(32);

        bool _isComboInputEnable = false;
        bool _isComboInputted = false;
        bool _isComboSuspended = false;

        protected IDisposable _attackHitDisposable;
        protected CompositeDisposable _disposables = new CompositeDisposable();

        [Inject]
        public void Construct(
            IAudioController audioController,
            ICombatSetting combatSetting
        )
        {
            _audioController = audioController;
            _combatSetting = combatSetting;
        }

        void ICombat.Init(IAttackComponentCollection attackOwner, IAnimationController animationController, PlayableAsset timeline)
        {
            InternalInit(attackOwner, animationController, timeline);
        }

        protected virtual void InternalInit(IAttackComponentCollection attackOwner, IAnimationController animationController, PlayableAsset timeline)
        {
            _attackOwner = attackOwner;
            //_skillEffect.SetActive(false);
            _animationController = animationController;

            _director.playableAsset = timeline;

            // AttackColliderをクリップから取得
            foreach (var trackAsset in (_director.playableAsset as TimelineAsset).GetOutputTracks())
            {
                if (trackAsset is CombatAttackTrack)
                {
                    if (_director.GetGenericBinding(trackAsset) is AttackCollider attackCollider)
                    {
                        _attackColliders.Add(attackCollider);
                    }
                }
            }
            foreach (var attackCollider in _attackColliders)
            {
                attackCollider.AttackApplicator = _attackOwner.AttackApplicator;
                attackCollider.gameObject.SetActive(false);
            }

            _animationController.OnFinishCombat
                .Subscribe(_ =>
                {
                    OnFinishCombat();
                }).AddTo(this);
        }

        void ICombat.Execute()
        {
            var isPlaying = (_director.state == PlayState.Playing);
            if (isPlaying && _isComboInputEnable)
            {
                _isComboInputted = true;
                return;
            }

            if (!isPlaying) InternalExecute();
        }

        protected virtual void InternalExecute()
        {
            _isComboInputEnable = false;
            _isComboInputted = false;
            _isComboSuspended = false;
            _attackColliders.ForEach(collider => collider.gameObject.SetActive(false));

            _director.Play();
            _animationController.Play(_director.playableAsset.name);
        }

        void ICombatAttack.OnAttackEnable(int damage, Vector3 colliderVelocity)
        {
            if (_isComboSuspended) return;
            OnAttackEnable(damage, colliderVelocity);
        }

        void ICombatAttack.OnAttackDisable()
        {
            if (_isComboSuspended) return;
            OnAttackDisable();
        }

        void IComboInputEnable.ComboInputEnable()
        {
            _isComboInputted = false;
            _isComboInputEnable = true;
        }

        void IComboInputEnable.ComboInputDisable()
        {
            if (!_isComboInputted)
            {
                _isComboSuspended = true;
                _animationController.Play("Idle");
            }
            _isComboInputEnable = false;
        }

        protected virtual void OnAttackEnable(int damage, Vector3 colliderVelocity)
        {
            // hit検知
            _attackHitDisposable?.Dispose();
            _attackHitDisposable = _attackOwner.AttackObservable.OnAttackHit
                .Subscribe(_ => OnAttackHit());

            // Attack
            var attackSetting = new CombatAttackSetting
            {
                power = damage,
                attackColliders = _attackColliders
            };
            _attackOwner.AttackController.StartAttack(attackSetting);
        }

        protected virtual void OnAttackHit()
        {
            _audioController.Play(_combatSetting.HitAudioName);
        }

        protected virtual void OnAttackDisable()
        {

        }

        protected virtual void FinishAttack()
        {
            _attackOwner.AttackController.FinishAttack();
            _attackHitDisposable?.Dispose();
        }

        protected virtual void OnFinishCombat()
        {
            // 終了処理
            _isComboInputEnable = false;

            _disposables.Clear();
            _director.Stop();

            _onFinishCombat.OnNext(Unit.Default);
        }

        void IDisposable.Dispose()
        {
            InternalDispose();
        }

        protected virtual void InternalDispose()
        {
            _attackHitDisposable?.Dispose();
            _disposables.Dispose();
        }
    }
}
