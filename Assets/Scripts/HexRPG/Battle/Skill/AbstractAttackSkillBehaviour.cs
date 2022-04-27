using UnityEngine;
using UnityEngine.Timeline;
using Zenject;
using UniRx;
using System;
using System.Collections.Generic;
using UnityEngine.Playables;

namespace HexRPG.Battle.Skill
{
    using Stage;
    using Playable;

    public abstract class AbstractAttackSkillBehaviour : MonoBehaviour, ISkillObservable, ISkill, IDisposable
    {
        IStageController _stageController;
        IAttackController _attackController;
        IBattleObservable _battleObservable;

        IAnimationController _animationController;

        [SerializeField] protected PlayableDirector _director;

        PlayableAsset ISkill.PlayableAsset => _director.playableAsset;

        List<Vector2> ISkill.FullAttackRange => _fullAttackRange;
        List<Vector2> _fullAttackRange;

        IReadOnlyReactiveProperty<Hex[]> ISkillObservable.OnSkillAttack => _onSkillAttack;
        readonly IReactiveProperty<Hex[]> _onSkillAttack = new ReactiveProperty<Hex[]>(new Hex[0]);

        IObservable<Unit> ISkillObservable.OnFinishSkill => _onFinishSkill;
        readonly ISubject<Unit> _onFinishSkill = new Subject<Unit>();

        ICharacterComponentCollection _skillOrigin;
        Hex[] _curAttackRange;
        Dictionary<string, GameObject> _skillEffectMap = new Dictionary<string, GameObject>();
        List<GameObject> _unverifiedEffect = new List<GameObject>(); // OnAttackDisableをまだ経過していない(Liberate未検証)->Timeline中断時に非表示にするエフェクト

        TrackAsset _cinemachineTrack;

        CompositeDisposable _disposables = new CompositeDisposable();

        [Inject]
        public void Construct(
            IStageController stageController,
            IAttackController attackController,
            IBattleObservable battleObservable
        )
        {
            _stageController = stageController;
            _attackController = attackController;
            _battleObservable = battleObservable;
        }

        void ISkill.Init(PlayableAsset timeline, ICharacterComponentCollection skillOrigin, IAnimationController animationController)
        {
            _skillOrigin = skillOrigin;
            _animationController = animationController;

            _director.playableAsset = timeline;

            // Skillの全範囲を取得
            foreach (var trackAsset in (_director.playableAsset as TimelineAsset).GetOutputTracks())
            {
                if (trackAsset is AttackEnableTrack)
                {
                    _fullAttackRange = new List<Vector2>();
                    foreach (var clip in trackAsset.GetClips())
                    {
                        var behaviour = (clip.asset as AttackEnableAsset).behaviour;
                        behaviour.attackRange.ForEach(range =>
                        {
                            if (!_fullAttackRange.Contains(range)) _fullAttackRange.Add(range);
                        });
                    }
                }
            }

            // Timelineの全Effectを取得
            foreach (var trackAsset in (_director.playableAsset as TimelineAsset).GetOutputTracks())
            {
                if (trackAsset is ActivationTrack)
                {
                    if (_director.GetGenericBinding(trackAsset) is GameObject skillEffect)
                    {
                        _skillEffectMap.Add(trackAsset.name, skillEffect);
                    }
                }
            }

            _animationController.OnFinishSkill
                .Subscribe(_ =>
                {
                    // 終了処理
                    FinishAttackEnable();
                    HideUnVerifiedEffect();
                    _unverifiedEffect.Clear();

                    _disposables.Clear();
                    _director.Stop();

                    _onFinishSkill.OnNext(Unit.Default);
                })
                .AddTo(this);
        }

        void ISkill.StartSkill(Hex landedHex, int skillRotation)
        {
            foreach(KeyValuePair<string, GameObject> item in _skillEffectMap) _unverifiedEffect.Add(item.Value);
            HideUnVerifiedEffect();

            //TODO: ↓いつか参考になりそう(isEnemyExistInSkillRange)
            //var isEnemyExistInSkillRange = _battleObservable.EnemyList.Any(enemy => skillRange.Contains(enemy.TransformController.GetLandedHex()));
            //_cinemachineTrack.muted = !isEnemyExistInSkillRange;

            foreach (var trackAsset in (_director.playableAsset as TimelineAsset).GetOutputTracks())
            {
                // Attack判定
                if (trackAsset is AttackEnableTrack)
                {
                    foreach (var clip in trackAsset.GetClips())
                    {
                        var behaviour = (clip.asset as AttackEnableAsset).behaviour;
                        behaviour.OnAttackEnable
                            .Subscribe(_ => {
                                _curAttackRange = _stageController.GetHexList(landedHex, behaviour.attackRange, skillRotation);
                                StartAttackEnable(behaviour.damage);
                            })
                            .AddTo(_disposables);
                        behaviour.OnAttackDisable
                            .Subscribe(_ =>
                            {
                                FinishAttackEnable();
                                _onSkillAttack.Value = _curAttackRange;
                                if (_skillEffectMap.TryGetValue(behaviour.attackEffectTrack, out GameObject effect)) _unverifiedEffect.Remove(effect);
                            })
                            .AddTo(_disposables);
                    }
                }
            }

            _director.Play();
            _animationController.Play(_director.playableAsset.name);
        }

        protected virtual void StartAttackEnable(int damage)
        {
            var attackSetting = new SkillAttackSetting
            {
                power = damage, 
                attackRange = _curAttackRange
            };

            _attackController.StartAttack(attackSetting, _skillOrigin);
        }

        protected virtual void FinishAttackEnable()
        {
            _attackController.FinishAttack();
        }

        public void HideUnVerifiedEffect()
        {
            _unverifiedEffect.ForEach(effect => effect.SetActive(false));
        }

        void IDisposable.Dispose()
        {
            _disposables.Dispose();
        }

        //TODO: 消すか判断
        void SetupCinemachine()
        {
            foreach (var bind in _director.playableAsset.outputs)
            {
                if (bind.streamName == "Cinemachine Track")
                {
                    _director.SetGenericBinding(bind.sourceObject, _battleObservable.CinemachineBrain);
                }
            }

            foreach (var trackAsset in (_director.playableAsset as TimelineAsset).GetOutputTracks())
            {
                if (trackAsset is CinemachineTrack)
                {
                    _cinemachineTrack = trackAsset;
                    foreach (var clip in _cinemachineTrack.GetClips())
                    {
                        if (clip.displayName == "Main CM vcam")
                        {
                            var cinemachineShot = clip.asset as CinemachineShot;
                            if (cinemachineShot != null)
                            {
                                _director.SetReferenceValue(cinemachineShot.VirtualCamera.exposedName, _battleObservable.MainVirtualCamera);
                            }
                        }
                    }
                }
            }
        }
    }
}
