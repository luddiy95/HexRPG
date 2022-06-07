using UnityEngine;
using UnityEngine.Timeline;
using Zenject;
using UniRx;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.Playables;

namespace HexRPG.Battle.Skill
{
    using Stage;
    using Playable;

    public class AbstractAttackSkillBehaviour : MonoBehaviour, ISkillObservable, ISkill, IDisposable
    {
        IStageController _stageController;
        IBattleObservable _battleObservable;

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

        IObservable<SkillAttackSetting> ISkillObservable.OnSkillAttackEnable => _onSkillAttackEnable;
        readonly ISubject<SkillAttackSetting> _onSkillAttackEnable = new Subject<SkillAttackSetting>();
        IObservable<Unit> ISkillObservable.OnSkillAttackDisable => _onSkillAttackDisable;
        readonly ISubject<Unit> _onSkillAttackDisable = new Subject<Unit>();

        IObservable<Hex[]> ISkillObservable.OnSkillAttack => _onSkillAttack;
        readonly ISubject<Hex[]> _onSkillAttack = new Subject<Hex[]>();

        IObservable<Unit> ISkillObservable.OnFinishSkill => _onFinishSkill;
        readonly ISubject<Unit> _onFinishSkill = new Subject<Unit>();

        Hex[] _curAttackRange;
        Dictionary<string, GameObject> _skillEffectMap = new Dictionary<string, GameObject>();
        List<GameObject> _unverifiedEffect = new List<GameObject>(); // OnAttackDisable���܂��o�߂��Ă��Ȃ�(Liberate������)->Timeline���f���ɔ�\���ɂ���G�t�F�N�g

        TrackAsset _cinemachineTrack;

        CompositeDisposable _disposables = new CompositeDisposable();

        [Inject]
        public void Construct(
            IStageController stageController,
            IBattleObservable battleObservable
        )
        {
            _stageController = stageController;
            _battleObservable = battleObservable;
        }

        void ISkill.Init(PlayableAsset timeline, List<ActivationBindingData> activationBindingMap, IAnimationController animationController)
        {
            _animationController = animationController;

            _director.playableAsset = timeline;

            var attackEffectTrack = new List<string>();
            // Skill�̑S�͈͂��擾
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

            // Effect�擾
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

            // ActivationTrack��Bind
            foreach (var bind in _director.playableAsset.outputs)
            {
                var data = activationBindingMap.FirstOrDefault(data => data.trackName == bind.streamName);
                if (data != null)
                {
                    _director.SetGenericBinding(bind.sourceObject, data.sourceObject);
                }
            }

            _animationController.OnFinishSkill
                .Subscribe(_ =>
                {
                    // �I������
                    _onSkillAttackDisable.OnNext(Unit.Default);
                    HideUnverifiedEffect();
                    _unverifiedEffect.Clear();
                    activationBindingMap.ForEach(data => data.sourceObject.SetActive(false));

                    _disposables.Clear();
                    _director.Stop();

                    _onFinishReservation.OnNext(Unit.Default);
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
                        // Attack����
                        var behaviour = (clip.asset as AttackEnableAsset).behaviour;
                        behaviour.OnAttackEnable
                            .Subscribe(_ => {
                                _curAttackRange = _stageController.GetHexList(skillCenter, behaviour.attackRange, skillRotation);

                                var attackSetting = new SkillAttackSetting
                                {
                                    power = behaviour.damage,
                                    attackRange = _curAttackRange
                                };
                                _onSkillAttackEnable.OnNext(attackSetting);

                                //TODO: �q�b�g������unverifiedEffect����remove����悤�ȍw�Ǔo�^��������(OnAttackDisable���ɉ���)
                                //TODO: ����PlayerOwner, EnemyOwner��n����AttackController, AttackObservable���������Ō����悤�ɂ��������悭�Ȃ��H
                                //TODO: -> ���̏ꍇICharacterComponentCollection�ɒǉ����邱�ƂɂȂ邪MemberOwner�͂���������Ȃ��̂ł�����Ɨǂ��Ȃ�
                                //TODO: -> Owner��n���̂ł͂Ȃ�AttackController, AttackObservable���ꂼ��n���΂悢
                                //TODO: IMemberComponentCollection��ICharacterComponentCollection���p�����Ȃ����@�́H
                                //TODO: ISkill��ISkillSetting�̎g���ǂ��낪�悭�킩���(����ISkillSetting)
                            })
                            .AddTo(_disposables);
                        behaviour.OnAttackDisable
                            .Subscribe(_ =>
                            {
                                _onSkillAttackDisable.OnNext(Unit.Default);

                                _onSkillAttack.OnNext(_curAttackRange); // ���e�����^�C�~���O��Liberate����
                                if (_skillEffectMap.TryGetValue(behaviour.attackEffectTrack, out GameObject effect)) // ���e������G�t�F�N�g���Ō�܂ōĐ�����
                                {
                                    _unverifiedEffect.Remove(effect);
                                }
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
                            .Subscribe(_ => _onFinishReservation.OnNext(Unit.Default))
                            .AddTo(_disposables);
                    }
                }
            }

            _director.Play();
            _animationController.Play(_director.playableAsset.name);
        }

        void HideUnverifiedEffect()
        {
            _unverifiedEffect.ForEach(effect => effect.SetActive(false));
        }

        void IDisposable.Dispose()
        {
            _disposables.Dispose();
        }

        //TODO: ���������f
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
