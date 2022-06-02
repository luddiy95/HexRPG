using System.Collections.Generic;
using System;
using System.Linq;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;
using UnityEngine.Timeline;
using UnityEditor;
using System.Threading;
using Cysharp.Threading.Tasks;
using UniRx;
using Zenject;

namespace HexRPG.Battle.Player.Member
{
    public class MemberAnimationBehaviour : AnimationBehaviour, IAnimationController
    {
        ICombatSpawnObservable _combatSpawnObservable;
        ISkillSpawnObservable _skillSpawnObservable;

        IObservable<Unit> IAnimationController.OnFinishDamaged => _onFinishDamaged;

        IObservable<Unit> IAnimationController.OnFinishCombat => _onFinishCombat;
        readonly ISubject<Unit> _onFinishCombat = new Subject<Unit>();

        IObservable<Unit> IAnimationController.OnFinishSkill => _onFinishSkill;
        
        // Combat
        class CombatTimelineInfo
        {
            public string CombatName { get; set; }
            public List<TimelineClipInfo> TimelineClipInfoList { get; set; } = new List<TimelineClipInfo>();
        }
        CombatTimelineInfo _combatTimelineInfo;
        CombatTimelineInfo _curCombat;

        [Inject]
        public void Construct(
            IProfileSetting profileSetting,
            IDieSetting dieSetting,
            IAnimatorController animatorController,
            ICombatSpawnObservable combatSpawnObservable,
            ISkillSpawnObservable skillSpawnObservable
        )
        {
            _profileSetting = profileSetting;
            _dieSetting = dieSetting;
            _animatorController = animatorController;
            _combatSpawnObservable = combatSpawnObservable;
            _skillSpawnObservable = skillSpawnObservable;
        }

        void IAnimationController.Init()
        {
            var name = _profileSetting.Name;
            _durationDataContainer = Resources.Load<DurationDataContainer>
                ("HexRPG/Battle/ScriptableObject/Player/" + name + "/" + name + "DurationDataContainer");

            SetupGraph();

            _animationTypeMap.Add(_playables[0].GetAnimationClip().name, AnimationType.Idle);
            var locomotionClipLength = AnimationExtensions.MoveClips.Length;
            for (int i = 1; i < locomotionClipLength + 1; i++)
            {
                _animationTypeMap.Add(_playables[i].GetAnimationClip().name, AnimationType.Move);
            }

            _animationTypeMap.Add("RotateRight", AnimationType.Rotate);
            _animationTypeMap.Add("RotateLeft", AnimationType.Rotate);
            //TODO: 仮
            var playerRotateSpeed = 0.5f;
            int index = _playables.FindIndex(x => x.GetAnimationClip().name == "RotateRight");
            _playables[index].SetSpeed(playerRotateSpeed);
            index = _playables.FindIndex(x => x.GetAnimationClip().name == "RotateLeft");
            _playables[index].SetSpeed(playerRotateSpeed);

            _animationTypeMap.Add("Damaged", AnimationType.Damaged);

            SetupDieAnimation();

            SetupCombatAnimation(_combatSpawnObservable.Combat.Combat.PlayableAsset);
            Array.ForEach(_skillSpawnObservable.SkillList, skill => SetupSkillAnimation(skill.Skill.PlayableAsset));

            _allClipCount = _playables.Count;
        }

        #region AnimationPlayer

        void IAnimationController.Play(string nextClip)
        {
            // 最初の遷移
            if (_curPlayingIndex < 0)
            {
                _curPlayingIndex = _playables.FindIndex(x => x.GetAnimationClip().name == nextClip);

                _mixer.SetInputWeight(_curPlayingIndex, 1);

                _mixer.SetTime(0);
                _playables[_curPlayingIndex].SetTime(0);

                return;
            }

            var curClip = _playables[_curPlayingIndex].GetAnimationClip().name;

            AnimationType type;
            if (_cancellationTokenSource == null) //! 「Animation間遷移 & Combat/Skill/Die待ち合わせ中」ではない
            {
                //! Damagedの場合のみ自分自身(Damaged)に遷移できる
                if(_playables[_curPlayingIndex].GetAnimationClip().name == "Damaged" && nextClip == "Damaged")
                {
                    _cancellationTokenSource = new CancellationTokenSource();
                    InternalAnimationTransit(nextClip, GetFadeLength(curClip, nextClip), _cancellationTokenSource.Token).Forget();
                    return;
                }

                //! 遷移中などでない場合、(Damaged -> Damaged)以外は自分自身には遷移しない
                if (_playables[_curPlayingIndex].GetAnimationClip().name == nextClip) return;

                // Combatですか？
                if (_combatTimelineInfo.CombatName == nextClip)
                {
                    if (_curCombat != null) return;

                    _cancellationTokenSource = new CancellationTokenSource();
                    InternalPlayCombat(_combatTimelineInfo, _cancellationTokenSource.Token).Forget(); // 待ち合わせする必要はない
                    return;
                }

                // Skillですか？
                var skillTimelineInfo = _skillTimelineInfos.FirstOrDefault(info => info.SkillName == nextClip);
                if (skillTimelineInfo != null)
                {
                    if (_curSkill != null) return;

                    _cancellationTokenSource = new CancellationTokenSource();
                    InternalPlaySkill(skillTimelineInfo, _cancellationTokenSource.Token).Forget(); // 待ち合わせする必要はない
                    return;
                }

                // Die
                var isDieClip = (_animationTypeMap.TryGetValue(nextClip, out type) && type == AnimationType.Die);
                if (isDieClip)
                {
                    _cancellationTokenSource = new CancellationTokenSource();
                    InternalPlayDie(_cancellationTokenSource.Token).Forget();
                    return;
                }

                _cancellationTokenSource = new CancellationTokenSource();
                InternalAnimationTransit(nextClip, GetFadeLength(curClip, nextClip), _cancellationTokenSource.Token).Forget();
            }
            else
            {
                //! 割り込み(非同期メソッド実行中 == 「CrossFade(アニメーション遷移中) || Combat/Skill/Die待ち合わせ中」)

                //! Damagedへ遷移中(_nextPlayingIndex = Damaged)のみ_nextPlayingIndex(Damaged)で割り込み可能
                if(_nextPlayingIndex >= 0 && _playables[_nextPlayingIndex].GetAnimationClip().name == "Damaged" && nextClip == "Damaged")
                {
                    TokenCancel();

                    if (_curCombat != null) FinishCombat();
                    if (_curSkill != null) FinishSkill();

                    if (_nextPlayingIndex >= 0) fixedRate = rate;

                    _cancellationTokenSource = new CancellationTokenSource();
                    InternalAnimationTransit(nextClip, GetFadeLength(curClip, nextClip), _cancellationTokenSource.Token).Forget(); // 待ち合わせる必要なし
                    return;
                }

                //! _nextPlayingIndexへ遷移中、_nextPlayingIndexで割り込みしない
                if (_nextPlayingIndex >= 0 && _playables[_nextPlayingIndex].GetAnimationClip().name == nextClip) return;

                // Locomotion->Locomotion遷移中は「Idle, Combat, Skill」割り込み可能
                var isCrossFadeBtwLocomotion =
                    (_animationTypeMap.TryGetValue(_playables[_curPlayingIndex].GetAnimationClip().name, out type) && type.IsLocomotionType()) &&
                    (_nextPlayingIndex >= 0 && _animationTypeMap.TryGetValue(_playables[_nextPlayingIndex].GetAnimationClip().name, out type) && type.IsLocomotionType());
                if (isCrossFadeBtwLocomotion)
                {
                    // Combatですか？
                    if (_combatTimelineInfo.CombatName == nextClip)
                    {
                        if (_curCombat != null) return;

                        // 割り込み
                        TokenCancel();
                        if (_nextPlayingIndex >= 0) fixedRate = rate;

                        _cancellationTokenSource = new CancellationTokenSource();
                        InternalPlayCombat(_combatTimelineInfo, _cancellationTokenSource.Token).Forget(); // 待ち合わせする必要はない
                        return;
                    }

                    // Skillですか？
                    var skillTimelineInfo = _skillTimelineInfos.FirstOrDefault(info => info.SkillName == nextClip);
                    if (skillTimelineInfo != null)
                    {
                        if (_curSkill != null) return;

                        // 割り込み
                        TokenCancel();
                        if (_nextPlayingIndex >= 0) fixedRate = rate;

                        _cancellationTokenSource = new CancellationTokenSource();
                        InternalPlaySkill(skillTimelineInfo, _cancellationTokenSource.Token).Forget(); // 待ち合わせする必要はない
                        return;
                    }

                    if (nextClip == "Idle")
                    {
                        // 割り込み
                        TokenCancel(); // Tokenキャンセルしたらawait後続処理は全て呼ばれない
                        if (_nextPlayingIndex >= 0) fixedRate = rate;

                        _cancellationTokenSource = new CancellationTokenSource();
                        InternalAnimationTransit(nextClip, GetFadeLength(curClip, nextClip), _cancellationTokenSource.Token).Forget(); // 待ち合わせる必要なし
                        return;
                    }
                }

                // Idle -> Rotate遷移中はIdle割り込み可能
                if(curClip == "Idle" && nextClip == "Idle" && _nextPlayingIndex >= 0 &&
                    _animationTypeMap.TryGetValue(_playables[_nextPlayingIndex].GetAnimationClip().name, out type) && type == AnimationType.Rotate)
                {
                    // 割り込み
                    TokenCancel(); // Tokenキャンセルしたらawait後続処理は全て呼ばれない
                    if (_nextPlayingIndex >= 0) fixedRate = rate;

                    _cancellationTokenSource = new CancellationTokenSource();
                    InternalAnimationTransit(nextClip, GetFadeLength(curClip, nextClip), _cancellationTokenSource.Token).Forget(); // 待ち合わせる必要なし
                    return;
                }

                // Damaged
                if (nextClip == "Damaged")
                {
                    TokenCancel();

                    if (_curCombat != null) FinishCombat();
                    if (_curSkill != null) FinishSkill();

                    if (_nextPlayingIndex >= 0) fixedRate = rate;

                    _cancellationTokenSource = new CancellationTokenSource();
                    InternalAnimationTransit(nextClip, GetFadeLength(curClip, nextClip), _cancellationTokenSource.Token).Forget();
                    return;
                }

                // Die
                var isDieClip = (_animationTypeMap.TryGetValue(nextClip, out type) && type == AnimationType.Die);
                if (isDieClip)
                {
                    TokenCancel();

                    if (_curCombat != null) FinishCombat();
                    if (_curSkill != null) FinishSkill();

                    if (_nextPlayingIndex >= 0) fixedRate = rate;

                    _cancellationTokenSource = new CancellationTokenSource();
                    InternalPlayDie(_cancellationTokenSource.Token).Forget();
                    return;
                }

                // Combat中断
                var isCombatSuspended = (_curCombat != null && nextClip == "Idle");
                if (isCombatSuspended)
                {
                    TokenCancel();
                    if (_nextPlayingIndex >= 0) fixedRate = rate;

                    _cancellationTokenSource = new CancellationTokenSource();
                    _cancellationTokenSource.Token.Register(() => FinishCombat());
                    InternalAnimationTransit(nextClip, GetFadeLength(curClip, nextClip), _cancellationTokenSource.Token).Forget();
                    return;
                }
            }
        }

        async UniTask InternalPlayCombat(CombatTimelineInfo combatTimelineInfo, CancellationToken token)
        {
            _curCombat = combatTimelineInfo;

            for (int i = 0; i < _curCombat.TimelineClipInfoList.Count; i++)
            {
                var timelineClipInfo = _curCombat.TimelineClipInfoList[i];

                var fadeLength = 0f;
                if (i == 0)
                {
                    fadeLength = _durationDataContainer.defaultCombatStartDuration;
                    var combatStartDurationData = _durationDataContainer.combatStartDurations.FirstOrDefault(data => data.clip == _curCombat.CombatName);
                    if (combatStartDurationData != null) fadeLength = combatStartDurationData.duration;
                }
                else if (timelineClipInfo.BlendInDuration >= 0)
                {
                    fadeLength = (float)timelineClipInfo.BlendInDuration;
                }

                var blendOutDuration = timelineClipInfo.BlendOutDuration;
                if (blendOutDuration < 0) blendOutDuration = 0f;
                var exitTime = (float)(timelineClipInfo.Duration - blendOutDuration);
                float exitWaitTime = Time.timeSinceLevelLoad + exitTime;

                await InternalCrossFade(timelineClipInfo.ClipName, fadeLength, token);

                await UniTask.WaitWhile(() =>
                {
                    var diff = exitWaitTime - Time.timeSinceLevelLoad;
                    return (diff > 0);
                }, cancellationToken: token);
            }

            // 割り込みがなかった場合のみここまで辿り着く
            TokenCancel();

            FinishCombat();
        }

        #endregion

        void FinishCombat()
        {
            _curCombat = null;
            _onFinishCombat.OnNext(Unit.Default);
        }

        #region Setup

        void SetupCombatAnimation(PlayableAsset playableAsset)
        {
            foreach (var trackAsset in (playableAsset as TimelineAsset).GetOutputTracks())
            {
                if (trackAsset is AnimationTrack)
                {
                    List<TimelineClip> clips = new List<TimelineClip>();
                    foreach (var clip in trackAsset.GetClips()) clips.Add(clip);

                    // combatTimelineInfoListに追加
                    List<TimelineClipInfo> timelinClipInfoList = new List<TimelineClipInfo>();
                    foreach (var clip in clips.OrderBy(clip => clip.start))
                    {
                        var clipName = clip.animationClip.name;

                        timelinClipInfoList.Add(new TimelineClipInfo
                        {
                            ClipName = clipName,
                            Duration = clip.duration,
                            BlendInDuration = clip.blendInDuration,
                            BlendOutDuration = clip.blendOutDuration,
                        });

                        if (!_playables.Any(playable => playable.GetAnimationClip().name == clipName))
                        {
                            _animationTypeMap.Add(clipName, AnimationType.Combat);
                            var playable = AnimationClipPlayable.Create(_graph, clip.animationClip);
                            playable.SetSpeed(clip.timeScale);
                            _playables.Add(playable);
                            _mixer.AddInput(playable, 0, 0);
                        }
                    }
                    _combatTimelineInfo = new CombatTimelineInfo
                    {
                        CombatName = playableAsset.name,
                        TimelineClipInfoList = timelinClipInfoList
                    };

                    trackAsset.muted = true;
                }
            }
        }

        #endregion

#if UNITY_EDITOR

        public override void SetupDamaged()
        {
            _graph = PlayableGraph.Create();
            _playables = _clips.Select(clip => AnimationClipPlayable.Create(_graph, clip)).ToList();
            var damagedClip = _playables.First(playable => playable.GetAnimationClip().name == "Damaged").GetAnimationClip();
            var damagedToIdleEvent = new AnimationEvent[] {
                new AnimationEvent()
                {
                    time = damagedClip.length * 0.9f,
                    functionName = "FadeToIdle"
                }
            };
            AnimationUtility.SetAnimationEvents(damagedClip, damagedToIdleEvent);
            _graph.Destroy();
        }

        [CustomEditor(typeof(MemberAnimationBehaviour))]
        public class MemberAnimationBehaviourInspector : Editor
        {
            private void OnEnable()
            {
            }

            private void OnDisable()
            {
            }

            public override void OnInspectorGUI()
            {
                base.OnInspectorGUI();

                ((MemberAnimationBehaviour)target).OnInspectorGUI();
            }
        }

#endif
    }
}
