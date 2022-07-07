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

    public class AbstractAttackSkillBehaviour : MonoBehaviour, ISkillObservable, ISkill, IDisposable
    {
        IStageController _stageController;
        IBattleObservable _battleObservable;
        ISkillSetting _skillSetting;

        IAttackComponentCollection _attackOwner;
        IAnimationController _animationController;

        [SerializeField] protected PlayableDirector _director;

        PlayableAsset ISkill.PlayableAsset => _director.playableAsset;

        List<Vector2> ISkill.FullAttackRange => _fullAttackRange;
        List<Vector2> _fullAttackRange;

        SkillCenterType ISkill.SkillCenterType => _skillCenterType;
        SkillCenterType _skillCenterType;

        Vector2 ISkill.SkillCenter => _skillCenter;
        Vector2 _skillCenter;

        IObservable<Unit> ISkillObservable.OnStartReservation => _onStartReservation;
        readonly ISubject<Unit> _onStartReservation = new Subject<Unit>();
        IObservable<Unit> ISkillObservable.OnFinishReservation => _onFinishReservation;
        readonly ISubject<Unit> _onFinishReservation = new Subject<Unit>();

        IObservable<IEnumerable<Hex>> ISkillObservable.OnSkillAttack => _onSkillAttack;
        readonly ISubject<IEnumerable<Hex>> _onSkillAttack = new Subject<IEnumerable<Hex>>();

        IObservable<Unit> ISkillObservable.OnFinishSkill => _onFinishSkill;
        readonly ISubject<Unit> _onFinishSkill = new Subject<Unit>();

        Hex[] _curAttackRange;
        Dictionary<string, GameObject> _skillEffectMap = new Dictionary<string, GameObject>();
        List<GameObject> _unverifiedEffect = new List<GameObject>(); // OnAttackDisableをまだ経過していない(Liberate未検証)->Timeline中断時に非表示にするエフェクト

        TrackAsset _cinemachineTrack;

        IDisposable _attackHitDisposable;
        CompositeDisposable _disposables = new CompositeDisposable();

        [Inject]
        public void Construct(
            IStageController stageController,
            IBattleObservable battleObservable,
            ISkillSetting skillSetting
        )
        {
            _stageController = stageController;
            _battleObservable = battleObservable;
            _skillSetting = skillSetting;
        }

        void ISkill.Init(
            IAttackComponentCollection attackOwner,
            IAnimationController animationController,
            PlayableAsset timeline,
            ActivationBindingObjDictionary activationBindingObjMap
        )
        {
            _attackOwner = attackOwner;
            _animationController = animationController;

            _director.playableAsset = timeline;

            var attackEffectTrack = new List<string>();
            // Skillの全範囲を取得
            foreach (var trackAsset in (_director.playableAsset as TimelineAsset).GetOutputTracks())
            {
                if (trackAsset is AttackEnableTrack attackEnableTrack)
                {
                    _fullAttackRange = new List<Vector2>();
                    _skillCenterType = attackEnableTrack.skillCenterType;
                    _skillCenter = attackEnableTrack.skillCenter;
                    foreach (var clip in attackEnableTrack.GetClips())
                    {
                        var behaviour = (clip.asset as AttackEnableAsset).behaviour;
                        attackEffectTrack.Add(behaviour.attackEffectTrack);
                        behaviour.attackRange.ForEach(range =>
                        {
                            if (!_fullAttackRange.Contains(range)) _fullAttackRange.Add(range);
                        });
                    }
                }
            }

            // Effect取得
            foreach (var trackAsset in (_director.playableAsset as TimelineAsset).GetOutputTracks())
            {
                if (trackAsset is ActivationTrack)
                {
                    if (attackEffectTrack.Contains(trackAsset.name))
                    {
                        if (_director.GetGenericBinding(trackAsset) is GameObject skillEffect)
                        {
                            _skillEffectMap.Add(trackAsset.name, skillEffect);
                        }
                    }
                }
            }

            // ActivationTrackのBind
            foreach (var bind in _director.playableAsset.outputs)
            {
                if (activationBindingObjMap.Table.TryGetValue(bind.streamName, out GameObject obj)) _director.SetGenericBinding(bind.sourceObject, obj);
            }

            _animationController.OnFinishSkill
                .Subscribe(_ =>
                {
                    // 終了処理
                    OnFinishReservation();
                    OnAttackDisable();

                    HideUnverifiedEffect();
                    _unverifiedEffect.Clear();

                    foreach (var data in activationBindingObjMap.Table) data.Value.SetActive(false);

                    _disposables.Clear();
                    _director.Stop();

                    _onFinishSkill.OnNext(Unit.Default);
                })
                .AddTo(this);
        }

        void ISkill.StartSkill(Hex skillCenter, int skillRotation)
        {
            foreach(KeyValuePair<string, GameObject> item in _skillEffectMap) _unverifiedEffect.Add(item.Value);
            HideUnverifiedEffect();

            //_cinemachineTrack.muted = !isEnemyExistInSkillRange;

            foreach (var trackAsset in (_director.playableAsset as TimelineAsset).GetOutputTracks())
            {
                if (trackAsset is AttackEnableTrack)
                {
                    foreach (var clip in trackAsset.GetClips())
                    {
                        // Attack判定
                        var behaviour = (clip.asset as AttackEnableAsset).behaviour;
                        behaviour.OnAttackEnable
                            .Subscribe(_ => {
                                _curAttackRange = _stageController.GetHexList(skillCenter, behaviour.attackRange, skillRotation);

                                var attackSetting = new SkillAttackSetting
                                {
                                    power = behaviour.damage,
                                    attackRange = _curAttackRange,
                                    attribute = _skillSetting.Attribute
                                };
                                _attackOwner.AttackController.StartAttack(attackSetting);

                                _attackHitDisposable?.Dispose();
                                _attackHitDisposable = _attackOwner.AttackObservable.OnAttackHit
                                    .Subscribe(_ => RemoveUnverifiedEffect(behaviour.attackEffectTrack));
                            })
                            .AddTo(_disposables);
                        behaviour.OnAttackDisable
                            .Subscribe(_ =>
                            {
                                OnAttackDisable();

                                _onSkillAttack.OnNext(_curAttackRange); // 着弾したタイミングでLiberate検証
                                RemoveUnverifiedEffect(behaviour.attackEffectTrack);
                            })
                            .AddTo(_disposables);

                        // AttackEffect
                        if(_skillEffectMap.TryGetValue(behaviour.attackEffectTrack, out GameObject effect))
                        {
                            effect.transform.position = _stageController.GetPos(skillCenter, behaviour.attackEffectOffset, skillRotation);
                            effect.transform.rotation = Quaternion.Euler(new Vector3(0, skillRotation, 0));
                        }
                    }
                }

                // SkillReservation
                if (trackAsset is SkillReservationTrack)
                {
                    foreach (var clip in trackAsset.GetClips())
                    {
                        var behaviour = (clip.asset as SkillReservationAsset).behaviour;
                        behaviour.OnStartReservation
                            .Subscribe(_ => _onStartReservation.OnNext(Unit.Default))
                            .AddTo(_disposables);
                        behaviour.OnFinishReservation
                            .Subscribe(_ => OnFinishReservation())
                            .AddTo(_disposables);
                    }
                }
            }

            _director.Play();
            _animationController.Play(_director.playableAsset.name);
        }

        void OnFinishReservation()
        {
            _onFinishReservation.OnNext(Unit.Default);
        }

        void OnAttackDisable()
        {
            _attackHitDisposable?.Dispose();
            _attackOwner.AttackController.FinishAttack();
        }

        void RemoveUnverifiedEffect(string effectTrackName)
        {
            if (_skillEffectMap.TryGetValue(effectTrackName, out GameObject effect)) // 着弾したらエフェクトを最後まで再生する
            {
                _unverifiedEffect.Remove(effect);
            }
        }

        void HideUnverifiedEffect()
        {
            _unverifiedEffect.ForEach(effect => effect.SetActive(false));
        }

        void IDisposable.Dispose()
        {
            _attackHitDisposable?.Dispose();
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
