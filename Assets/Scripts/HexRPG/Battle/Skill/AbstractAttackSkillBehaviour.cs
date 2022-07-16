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

    public class AbstractAttackSkillBehaviour : MonoBehaviour, ISkillObservable, ISkill, ISkillReservation, ISkillAttack, IDisposable
    {
        IStageController _stageController;
        IBattleObservable _battleObservable;
        ISkillSetting _skillSetting;

        IAttackComponentCollection _attackOwner;
        IAnimationController _animationController;

        [SerializeField] protected PlayableDirector _director;

        PlayableAsset ISkill.PlayableAsset => _director.playableAsset;

        List<Vector2> ISkill.FullAttackRange => _fullAttackRange;
        readonly List<Vector2> _fullAttackRange = new List<Vector2>(16);

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

        Hex _curSkillCenter;
        int _curSkillRotation;
        List<Hex> _curAttackRange = new List<Hex>(16);

        readonly Dictionary<string, GameObject> _skillEffectMap = new Dictionary<string, GameObject>(8);
        readonly List<GameObject> _unverifiedEffect = new List<GameObject>(8); // OnAttackDisableをまだ経過していない(Liberate未検証)->Timeline中断時に非表示にするエフェクト

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

            var attackEffectTrack = new List<string>(8);
            // Skillの全範囲を取得
            foreach (var trackAsset in (_director.playableAsset as TimelineAsset).GetOutputTracks())
            {
                if (trackAsset is SkillAttackTrack skillAttackTrack)
                {
                    _skillCenterType = skillAttackTrack.skillCenterType;
                    _skillCenter = skillAttackTrack.skillCenter;
                    foreach (var clip in skillAttackTrack.GetClips())
                    {
                        var asset = (clip.asset as SkillAttackAsset);
                        attackEffectTrack.Add(asset.attackEffectTrack);
                        asset.attackRange.ForEach(range =>
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
                    FinishAttack();

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
            _curSkillCenter = skillCenter;
            _curSkillRotation = skillRotation;

            foreach (KeyValuePair<string, GameObject> item in _skillEffectMap) _unverifiedEffect.Add(item.Value);
            HideUnverifiedEffect();

            //_cinemachineTrack.muted = !isEnemyExistInSkillRange;

            foreach (var trackAsset in (_director.playableAsset as TimelineAsset).GetOutputTracks())
            {
                if (trackAsset is SkillAttackTrack)
                {
                    foreach (var clip in trackAsset.GetClips())
                    {
                        var asset = clip.asset as SkillAttackAsset;
                        if (_skillEffectMap.TryGetValue(asset.attackEffectTrack, out GameObject effect))
                        {
                            effect.transform.position = _stageController.GetPos(_curSkillCenter, asset.attackEffectOffset, _curSkillRotation);
                            effect.transform.rotation = Quaternion.Euler(new Vector3(0, _curSkillRotation, 0));
                        }
                    }
                }
            }

            _director.Play();
            _animationController.Play(_director.playableAsset.name);
        }

        void ISkillReservation.OnStartReservation()
        {
            _onStartReservation.OnNext(Unit.Default);
        }

        void ISkillReservation.OnFinishReservation()
        {
            OnFinishReservation();
        }

        void ISkillAttack.OnAttackEnable(int damage, string attackEffectTrack, List<Vector2> attackRange, Vector2 attackEffectOffset)
        {
            OnAttackEnable(damage, attackEffectTrack, attackRange, attackEffectOffset);
        }

        void ISkillAttack.OnAttackDisable(string attackEffectTrack)
        {
            OnAttackDisable(attackEffectTrack);
        }

        void OnFinishReservation()
        {
            _onFinishReservation.OnNext(Unit.Default);
        }

        protected virtual void OnAttackEnable(int damage, string attackEffectTrack, List<Vector2> attackRange, Vector2 attackEffectOffset)
        {
            _stageController.GetHexList(_curSkillCenter, attackRange, _curSkillRotation, ref _curAttackRange);

            var attackSetting = new SkillAttackSetting
            {
                power = damage,
                attackRange = _curAttackRange,
                attribute = _skillSetting.Attribute
            };
            _attackOwner.AttackController.StartAttack(attackSetting);

            _attackHitDisposable?.Dispose();
            _attackHitDisposable = _attackOwner.AttackObservable.OnAttackHit
                .Subscribe(_ => RemoveUnverifiedEffect(attackEffectTrack));
        }

        protected virtual void OnAttackDisable(string attackEffectTrack)
        {
            FinishAttack();

            _onSkillAttack.OnNext(_curAttackRange); // 着弾したタイミングでLiberate検証
            RemoveUnverifiedEffect(attackEffectTrack);
        }

        protected virtual void FinishAttack()
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
