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

    public class AbstractAttackSkillBehaviour : MonoBehaviour, ISkillObservable, ISkill, IAttackSkill
    {
        ISkillSetting _skillSetting;
        IAttackController _attackController;
        IBattleObservable _battleObservable;

        [SerializeField] protected GameObject _skillEffect;
        [SerializeField] protected PlayableDirector _director;
        TrackAsset _cinemachineTrack;

        IObservable<Unit> ISkillObservable.OnStartSkill => _onStartSkill;
        ISubject<Unit> _onStartSkill = new Subject<Unit>();
        IObservable<Unit> ISkillObservable.OnFinishSkill => _onFinishSkill;
        ISubject<Unit> _onFinishSkill = new Subject<Unit>();

        ICharacterComponentCollection _skillOrigin;
        List<Hex> _curSkillRange;

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

        void ISkill.Init(PlayableAsset timeline, ICharacterComponentCollection skillOrigin, Animator animator)
        {
            _skillEffect.SetActive(false);
            _skillOrigin = skillOrigin;

            _director.playableAsset = timeline;

            foreach (var bind in _director.playableAsset.outputs)
            {
                if (bind.streamName == "Animation Track")
                {
                    _director.SetGenericBinding(bind.sourceObject, animator);
                }

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

            _director.played += ((obj) => _onStartSkill.OnNext(Unit.Default));
            _director.stopped += ((obj) => _onFinishSkill.OnNext(Unit.Default));
        }

        void ISkill.StartSkill(List<Hex> skillRange)
        {
            _skillEffect.SetActive(false);
            _curSkillRange = skillRange;

            var isEnemyExistInSkillRange = _battleObservable.EnemyList.Any(enemy => skillRange.Contains(enemy.TransformController.GetLandedHex()));
            _cinemachineTrack.muted = !isEnemyExistInSkillRange;

            _director.Play();
        }

        public virtual void StartAttackEnable()
        {
            var attackSetting = new AttackSetting
            {
                _power = _skillSetting.Damage
            };

            _attackController.StartAttack(_curSkillRange, attackSetting, _skillOrigin);
        }

        public virtual void FinishAttackEnable()
        {
            _attackController.FinishAttack();
        }
    }
}
