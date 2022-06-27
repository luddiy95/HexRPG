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

    public abstract class AbstractCombatBehaviour : MonoBehaviour, ICombat, ICombatObservable, IDisposable
    {
        ICombatSetting _combatSetting;

        protected IAttackComponentCollection _attackOwner;
        IAnimationController _animationController;

        [SerializeField] protected PlayableDirector _director;

        IObservable<Unit> ICombatObservable.OnFinishCombat => _onFinishCombat;
        protected readonly ISubject<Unit> _onFinishCombat = new Subject<Unit>();

        PlayableAsset ICombat.PlayableAsset => _director.playableAsset;

        Vector3 ICombat.Velocity => velocity;
        Vector3 velocity = Vector3.zero;

        protected List<AttackCollider> _attackColliders = new List<AttackCollider>();

        bool _isComboInputEnable = false;
        bool _isComboInputted = false;
        bool _isComboSuspended = false;

        protected IDisposable _attackHitDisposable;
        protected CompositeDisposable _disposables = new CompositeDisposable();

        [Inject]
        public void Construct(
            ICombatSetting combatSetting
        )
        {
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
                if (trackAsset is AttackColliderTrack)
                {
                    if (_director.GetGenericBinding(trackAsset) is AttackCollider attackCollider)
                    {
                        _attackColliders.Add(attackCollider);
                    }
                }
            }
            _attackColliders.ForEach(attackCollider =>
            {
                attackCollider.AttackApplicator = _attackOwner.AttackApplicator;
                attackCollider.gameObject.SetActive(false);
            });

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
            _isComboSuspended = false;
            _attackColliders.ForEach(collider => collider.gameObject.SetActive(false));

            foreach (var trackAsset in (_director.playableAsset as TimelineAsset).GetOutputTracks())
            {
                // Velocity取得
                if (trackAsset is VelocityTrack)
                {
                    foreach (var clip in trackAsset.GetClips())
                    {
                        var behaviour = (clip.asset as VelocityAsset).behaviour;
                        behaviour.Velocity
                            .Where(_ => !_isComboSuspended) //! Combat中断してIdleへ遷移中はまだOnFinishCombatしていないためCombatステートをExit出来ずVelocity=0にならない
                            .Subscribe(velocity =>
                            {
                                this.velocity = velocity;
                            })
                            .AddTo(_disposables);
                    }
                }

                // Attack判定
                if (trackAsset is AttackColliderTrack)
                {
                    foreach (var clip in trackAsset.GetClips())
                    {
                        var behaviour = (clip.asset as AttackColliderAsset).behaviour;
                        behaviour.OnAttackEnable
                            .Subscribe(_ => OnAttackEnable(behaviour.damage, behaviour.Velocity))
                            .AddTo(_disposables);
                        behaviour.OnAttackDisable
                            .Subscribe(_ => OnAttackDisable())
                            .AddTo(_disposables);
                    }
                }

                // コンボ入力
                if (trackAsset is ComboInputEnableTrack)
                {
                    foreach (var clip in trackAsset.GetClips())
                    {
                        var behaviour = (clip.asset as ComboInputEnableAsset).behaviour;
                        behaviour.OnComboInputEnable
                            .Subscribe(_ =>
                            {
                                _isComboInputted = false;
                                _isComboInputEnable = true;
                            })
                            .AddTo(_disposables);
                        behaviour.OnComboInputDisable
                            .Subscribe(_ =>
                            {
                                if (!_isComboInputted)
                                {
                                    _isComboSuspended = true;
                                    _animationController.Play("Idle");
                                }
                                _isComboInputEnable = false;
                            })
                            .AddTo(_disposables);
                    }
                }
            }

            _director.Play();
            _animationController.Play(_director.playableAsset.name);
        }

        protected virtual void OnAttackEnable(int damage, Vector3 colliderVelocity)
        {
            // Attack
            var attackSetting = new CombatAttackSetting
            {
                power = damage,
                attackColliders = _attackColliders
            };
            _attackOwner.AttackController.StartAttack(attackSetting);

            // hit検知
            _attackHitDisposable?.Dispose();
            _attackHitDisposable = _attackOwner.AttackObservable.OnAttackHit
                .Subscribe(_ => OnAttackHit());
        }

        protected virtual void OnAttackHit()
        {

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
            //! velocityはActionStateControllerでCombatStateExit時に0になる

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
