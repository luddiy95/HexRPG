using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;
using System;
using UniRx;
using Zenject;

namespace HexRPG.Battle.Player.Combat
{
    using Playable;

    public class AbstractCombatBehaviour : MonoBehaviour, ICombat, ICombatObservable, IDisposable
    {
        IAttackController _attackController;
        IAttackApplicator _attackApplicator;
        ICombatSetting _combatSetting;

        [SerializeField] protected PlayableDirector _director;

        IObservable<Unit> ICombatObservable.OnFinishCombat => _onFinishCombat;
        readonly ISubject<Unit> _onFinishCombat = new Subject<Unit>();

        IReadOnlyReactiveProperty<Vector3> ICombatObservable.Velocity => _velocity;
        readonly IReactiveProperty<Vector3> _velocity = new ReactiveProperty<Vector3>();

        PlayableAsset ICombat.PlayableAsset => _director.playableAsset;

        Vector3 ICombat.Velocity => velocity;
        Vector3 velocity = Vector3.zero;

        [SerializeField] AttackCollider[] _attackColliders;
        ICharacterComponentCollection _combatOrigin;

        bool _isComboInputEnable = false;
        bool _isComboInputted = false;

        CompositeDisposable _disposables = new CompositeDisposable();

        [Inject]
        public void Construct(
            IAttackController attackController,
            IAttackApplicator attackApplicator,
            ICombatSetting combatSetting
        )
        {
            _attackController = attackController;
            _attackApplicator = attackApplicator;
            _combatSetting = combatSetting;
        }

        void ICombat.Init(PlayableAsset timeline, ICharacterComponentCollection combatOrigin, Animator animator)
        {
            Array.ForEach(_attackColliders, attackCollider => attackCollider.AttackApplicator = _attackApplicator);

            //_skillEffect.SetActive(false);
            _combatOrigin = combatOrigin;

            _director.playableAsset = timeline;

            // AnimatorÝ’è
            foreach (var bind in _director.playableAsset.outputs)
            {
                if (bind.streamName == "Animation Track")
                {
                    _director.SetGenericBinding(bind.sourceObject, animator);
                }
            }

            // TimelineI—¹ˆ—
            _director.stopped += (obj) => {
                _onFinishCombat.OnNext(Unit.Default);
                _disposables.Clear();
            };
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
                    // VelocityŽæ“¾
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

                    // Attack”»’è
                    if (trackAsset is AttackColliderTrack)
                    {
                        foreach (var clip in trackAsset.GetClips())
                        {
                            var behaviour = (clip.asset as AttackColliderAsset).behaviour;
                            behaviour.OnAttackEnable
                                .Subscribe(_ => StartAttackEnable())
                                .AddTo(_disposables);
                            behaviour.OnAttackDisable
                                .Subscribe(_ => FinishAttackEnable())
                                .AddTo(_disposables);
                        }
                    }

                    // ƒRƒ“ƒ{“ü—Í
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
                                        _director.Stop();
                                    }
                                    _isComboInputEnable = false;
                                })
                                .AddTo(_disposables);
                        }
                    }
                }

                _director.Play();
            }
        }

        protected virtual void StartAttackEnable()
        {
            var attackSetting = new CombatAttackSetting
            {
                _power = _combatSetting.Damage
            };
            _attackController.StartAttack(attackSetting, _combatOrigin);
        }

        protected virtual void FinishAttackEnable()
        {
            _attackController.FinishAttack();
        }

        void IDisposable.Dispose()
        {
            _disposables.Dispose();
        }
    }
}
