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

    public class AbstractCombatBehaviour : MonoBehaviour, ICombat, ICombatObservable, IDisposable
    {
        ICombatSetting _combatSetting;

        IAttackComponentCollection _attackOwner;
        IAnimationController _animationController;

        [SerializeField] protected PlayableDirector _director;

        IObservable<Unit> ICombatObservable.OnFinishCombat => _onFinishCombat;
        readonly ISubject<Unit> _onFinishCombat = new Subject<Unit>();

        PlayableAsset ICombat.PlayableAsset => _director.playableAsset;

        Vector3 ICombat.Velocity => velocity;
        Vector3 velocity = Vector3.zero;

        protected List<AttackCollider> _attackColliders = new List<AttackCollider>();

        bool _isComboInputEnable = false;
        bool _isComboInputted = false;

        IDisposable _attackHitDisposable;
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
            _attackColliders.ForEach(attackCollider => attackCollider.AttackApplicator = _attackOwner.AttackApplicator);

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
            foreach (var trackAsset in (_director.playableAsset as TimelineAsset).GetOutputTracks())
            {
                // Velocity取得
                if (trackAsset is VelocityTrack)
                {
                    foreach (var clip in trackAsset.GetClips())
                    {
                        var behaviour = (clip.asset as VelocityAsset).behaviour;
                        behaviour.Velocity
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
                            .Subscribe(_ =>
                            {
                                var attackSetting = new CombatAttackSetting
                                {
                                    power = _combatSetting.Damage,
                                    attackColliders = _attackColliders
                                };
                                _attackOwner.AttackController.StartAttack(attackSetting);

                                _attackHitDisposable?.Dispose();
                                _attackHitDisposable = _attackOwner.AttackObservable.OnAttackHit
                                    .Subscribe(_ => OnAttackHit());
                            })
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

        protected virtual void OnAttackHit()
        {

        }

        protected virtual void OnFinishCombat()
        {
            // 終了処理
            _isComboInputEnable = false;
            OnAttackDisable();
            // velocityはActionStateControllerでCombatStateExit時に0になる

            _disposables.Clear();
            _director.Stop();

            _onFinishCombat.OnNext(Unit.Default);
        }

        protected void OnAttackDisable()
        {
            _attackHitDisposable?.Dispose();
            _attackOwner.AttackController.FinishAttack();
        }

        void IDisposable.Dispose()
        {
            _attackHitDisposable?.Dispose();
            _disposables.Dispose();
        }
    }
}
