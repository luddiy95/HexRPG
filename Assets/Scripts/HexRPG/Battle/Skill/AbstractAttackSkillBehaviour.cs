using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Timeline;
using Zenject;
using UniRx;
using System;
using System.Linq;
using UnityEngine.Playables;

namespace HexRPG.Battle.Skill
{
    using Stage;
    using Playable;

    public class AbstractAttackSkillBehaviour : MonoBehaviour, ISkillObservable, ISkill, IDisposable
    {
        ISkillSetting _skillSetting;
        IAttackController _attackController;
        IBattleObservable _battleObservable;

        [SerializeField] protected GameObject _skillEffect;
        [SerializeField] protected PlayableDirector _director;

        PlayableAsset ISkill.PlayableAsset => _director.playableAsset;

        TrackAsset _cinemachineTrack;

        IObservable<Unit> ISkillObservable.OnFinishSkill => _onFinishSkill;
        readonly ISubject<Unit> _onFinishSkill = new Subject<Unit>();

        ICharacterComponentCollection _skillOrigin;
        List<Hex> _curSkillRange;

        IAnimationController _memberAnimationController;

        CompositeDisposable _disposables = new CompositeDisposable();

        [Inject]
        public void Construct(
            ISkillSetting skillSetting,
            IAttackController attackController,
            IBattleObservable battleObservable
        )
        {
            _skillSetting = skillSetting;
            _attackController = attackController;
            _battleObservable = battleObservable;
        }

        void Awake()
        {
            _skillEffect.SetActive(false);
        }

        void ISkill.Init(PlayableAsset timeline, ICharacterComponentCollection skillOrigin, IAnimationController memberAnimationController)
        {
            _skillEffect.SetActive(false);
            _skillOrigin = skillOrigin;
            _memberAnimationController = memberAnimationController;

            _director.playableAsset = timeline;

            _memberAnimationController.OnFinishSkill
                .Subscribe(_ =>
                {
                    // èIóπèàóù
                    FinishAttackEnable();
                    _disposables.Clear();
                    _director.Stop();

                    _onFinishSkill.OnNext(Unit.Default);
                })
                .AddTo(this);

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

        void ISkill.StartSkill(List<Hex> skillRange)
        {
            _skillEffect.SetActive(false);
            _curSkillRange = skillRange;

            var isEnemyExistInSkillRange = _battleObservable.EnemyList.Any(enemy => skillRange.Contains(enemy.TransformController.GetLandedHex()));
            //_cinemachineTrack.muted = !isEnemyExistInSkillRange;

            // AttackîªíË
            foreach (var trackAsset in (_director.playableAsset as TimelineAsset).GetOutputTracks())
            {
                if (trackAsset is AttackEnableTrack)
                {
                    foreach (var clip in trackAsset.GetClips())
                    {
                        var behaviour = (clip.asset as AttackEnableAsset).behaviour;
                        behaviour.OnAttackEnable
                            .Subscribe(_ => StartAttackEnable())
                            .AddTo(_disposables);
                        behaviour.OnAttackDisable
                            .Subscribe(_ => FinishAttackEnable())
                            .AddTo(_disposables);
                    }
                }
            }

            _director.Play();
            _memberAnimationController.Play(_director.playableAsset.name);
        }

        protected virtual void StartAttackEnable()
        {
            var attackSetting = new SkillAttackSetting
            {
                _power = _skillSetting.Damage, _attackRange = _curSkillRange
            };

            _attackController.StartAttack(attackSetting, _skillOrigin);
        }

        protected virtual void FinishAttackEnable()
        {
            _attackController.FinishAttack();
        }

        public void HideEffect()
        {
            _skillEffect.SetActive(false);
        }

        void IDisposable.Dispose()
        {
            _disposables.Dispose();
        }
    }
}
