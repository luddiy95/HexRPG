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
        IAttackApplicator _attackApplicator;
        ICombatSetting _combatSetting;

        [SerializeField] protected PlayableDirector _director;

        IObservable<CombatAttackSetting> ICombatObservable.OnCombatAttackEnable => _onCombatAttackEnable;
        readonly ISubject<CombatAttackSetting> _onCombatAttackEnable = new Subject<CombatAttackSetting>();
        IObservable<Unit> ICombatObservable.OnCombatAttackDisable => _onCombatAttackDisable;
        readonly ISubject<Unit> _onCombatAttackDisable = new Subject<Unit>();

        IObservable<Unit> ICombatObservable.OnFinishCombat => _onFinishCombat;
        readonly ISubject<Unit> _onFinishCombat = new Subject<Unit>();

        PlayableAsset ICombat.PlayableAsset => _director.playableAsset;

        Vector3 ICombat.Velocity => velocity;
        Vector3 velocity = Vector3.zero;

        List<AttackCollider> _attackColliders = new List<AttackCollider>();

        IAnimationController _animationController;

        bool _isComboInputEnable = false;
        bool _isComboInputted = false;

        CompositeDisposable _disposables = new CompositeDisposable();

        [Inject]
        public void Construct(
            IAttackApplicator attackApplicator,
            ICombatSetting combatSetting
        )
        {
            _attackApplicator = attackApplicator;
            _combatSetting = combatSetting;
        }

        void ICombat.Init(PlayableAsset timeline, IAttackApplicator attackApplicator, IAnimationController animationController)
        {
            _attackApplicator = attackApplicator;
            //_skillEffect.SetActive(false);
            _animationController = animationController;

            _director.playableAsset = timeline;

            // AttackColliderをクリップから取得
            foreach (var trackAsset in (_director.playableAsset as TimelineAsset).GetOutputTracks())
            {
                if (trackAsset is AttackColliderTrack)
                {
                    if(_director.GetGenericBinding(trackAsset) is AttackCollider attackCollider)
                    {
                        _attackColliders.Add(attackCollider);
                    }
                }
            }
            _attackColliders.ForEach(attackCollider => attackCollider.AttackApplicator = _attackApplicator);

            _animationController.OnFinishCombat
                .Subscribe(_ =>
                {
                    // 終了処理
                    _isComboInputEnable = false;
                    _onCombatAttackDisable.OnNext(Unit.Default);
                    // velocityはActionStateControllerでCombatStateExit時に0になる

                    _disposables.Clear();
                    _director.Stop();

                    _onFinishCombat.OnNext(Unit.Default);
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

            if(!isPlaying)
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
                                    _onCombatAttackEnable.OnNext(attackSetting);
                                })
                                .AddTo(_disposables);
                            behaviour.OnAttackDisable
                                .Subscribe(_ => _onCombatAttackDisable.OnNext(Unit.Default))
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
        }

        void IDisposable.Dispose()
        {
            _disposables.Dispose();
        }
    }
}
