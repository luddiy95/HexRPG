using System.Collections;
using System.Collections.Generic;
using System;
using System.Linq;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;
using UnityEngine.Timeline;
using System.Threading;
using Cysharp.Threading.Tasks;
using Zenject;

namespace HexRPG.Battle.Player
{
    using Member;

    //TODO: PlayerAnimatorBehaviourいらない
    public class MemberAnimationBehaviour : MonoBehaviour, IAnimationController
    {
        IAnimatorController _animatorController;
        ICombatSpawnObservable _combatSpawnObservable;
        ISkillSpawnObservable _skillSpawnObservable;

        [SerializeField] AnimationClip[] _clips;

        const int _locomotionCount = 9;
        List<string> _animationClips = new List<string>(); //TODO: _playablesがあるからいらないのでは？
        Dictionary<string, AnimationType> _animationTypeMap = new Dictionary<string, AnimationType>(); //TODO: このmapをどう作るか

        PlayableGraph _graph;
        List<AnimationClipPlayable> _playables;
        AnimationMixerPlayable _mixer;

        struct TimelineClipInfo
        {
            public string ClipName { get; set; }
            public double Duration { get; set; } // Animation全体の長さ(s)(本来の長さにSpeedを掛けたもの、実際にかかる時間)
            public double Speed { get; set; }
            public double BlendInDuration { get; set; }
            public double BlendOutDuration { get; set; }
        }
        
        class CombatTimelineInfo
        {
            public string CombatName { get; set; }
            public List<TimelineClipInfo> TimelineClipInfoList { get; set; } = new List<TimelineClipInfo>();
        }
        CombatTimelineInfo _combatTimelineInfo;

        class SkillTimelineInfo
        {
            public string SkillName { get; set; }
            public List<TimelineClipInfo> TimelineClipInfoList { get; set; } = new List<TimelineClipInfo>();
        }
        List<SkillTimelineInfo> _skillTImelineInfos;

        int _prePlayingIndex = -1, _curPlayingIndex = -1, _nextPlayingIndex = -1;

        float rate = 0f;
        float fixedRate = 0f; // 遷移中に割り込みが発生したときに本来の遷移がどの程度だったか

        CancellationTokenSource _cancellationTokenSource;

        [Inject]
        public void Construct(
            IAnimatorController animatorController,
            ICombatSpawnObservable combatSpawnObservable,
            ISkillSpawnObservable skillSpawnObservable
        )
        {
            _animatorController = animatorController;
            _combatSpawnObservable = combatSpawnObservable;
            _skillSpawnObservable = skillSpawnObservable;
        }

        //TODO: 各memberのAnimator, Combat, Skillを使うからCombat/SkillSpawnが終わってから呼ぶ
        void IAnimationController.Init()
        {
            _cancellationTokenSource?.Cancel();
            _cancellationTokenSource?.Dispose();
            _cancellationTokenSource = null;

            SetupGraph();

            SetupCombatAnimation(_combatSpawnObservable.Combat.Combat.PlayableAsset);
            Array.ForEach(_skillSpawnObservable.SkillList, skill => SetupSkillAnimation(skill.Skill.PlayableAsset));
        }

        void SetupGraph()
        {
            _graph = PlayableGraph.Create();
            _playables = _clips.Select(clip => AnimationClipPlayable.Create(_graph, clip)).ToList();
            _mixer = AnimationMixerPlayable.Create(_graph, _playables.Count, normalizeWeights: true);

            for (int i = 0; i < _playables.Count; ++i)
            {
                _mixer.ConnectInput(i, _playables[i], 0);
            }

            var output = AnimationPlayableOutput.Create(_graph, "AnimationPlayer", _animatorController.Animator);
            output.SetSourcePlayable(_mixer);
            _graph.Play();
        }

        void SetupCombatAnimation(PlayableAsset playableAsset)
        {
            //TODO: この辺はCombatの方で実行するべき
            /*
            combatDirector.stopped += (obj) =>
            {
                // _isComboInputEnable = false;
                // AttackColliderをdisableにする
                // LocomotionBehaviour#SetSpeed(0)にする(?)
            };
            */

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
                            Speed = clip.timeScale,
                            BlendInDuration = clip.blendInDuration,
                            BlendOutDuration = clip.blendOutDuration,
                        });

                        if (!_animationClips.Contains(clipName))
                        {
                            _animationClips.Add(clipName);
                            _animationTypeMap.Add(clipName, AnimationType.Combat);
                            var playable = AnimationClipPlayable.Create(_graph, clip.animationClip);
                            _playables.Add(playable);
                            _mixer.AddInput(playable, 0, 0); // mixerに接続
                        }
                    }
                    _combatTimelineInfo = new CombatTimelineInfo
                    {
                        CombatName = playableAsset.name,
                        TimelineClipInfoList = timelinClipInfoList
                    };

                    trackAsset.muted = true;
                }

                //TODO: コンボ中断の通知を受け取る->_animationPlayer.Play("Idle", 0.5f);
                // コンボ入力
                /*
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
                            .AddTo(this);
                        behaviour.OnComboInputDisable
                            .Subscribe(_ =>
                            {
                                if (!_isComboInputted)
                                {
                                        // Combat中断
                                        _animationPlayer.Play("Idle", 0.5f);
                                }
                                _isComboInputEnable = false;
                            })
                            .AddTo(this);
                    }
                }
                */
            }
        }

        void SetupSkillAnimation(PlayableAsset playableAsset)
        {
            //TODO: この辺はCombatの方で実行するべき
            /*
            _skillDirector.stopped += (obj) =>
            {
                //TODO: Stop時ではなく再生中のエフェクトが終了するまで続ける？
                //TODO: Stop時にdisableにする
            };
            */

            foreach (var trackAsset in (playableAsset as TimelineAsset).GetOutputTracks())
            {
                if (trackAsset is AnimationTrack)
                {
                    List<TimelineClip> clips = new List<TimelineClip>();
                    foreach (var clip in trackAsset.GetClips()) clips.Add(clip);

                    // skillTimelineInfoListに追加
                    List<TimelineClipInfo> timelinClipInfoList = new List<TimelineClipInfo>();
                    foreach (var clip in clips.OrderBy(clip => clip.start))
                    {
                        var clipName = clip.animationClip.name;

                        timelinClipInfoList.Add(new TimelineClipInfo
                        {
                            ClipName = clipName,
                            Duration = clip.duration,
                            Speed = clip.timeScale,
                            BlendInDuration = clip.blendInDuration,
                            BlendOutDuration = clip.blendOutDuration,
                        });

                        if (!_animationClips.Contains(clipName))
                        {
                            _animationClips.Add(clipName);
                            _animationTypeMap.Add(clipName, AnimationType.Skill);
                            var playable = AnimationClipPlayable.Create(_graph, clip.animationClip);
                            _playables.Add(playable);
                            _mixer.AddInput(playable, 0, 0); // mixerに接続
                        }
                    }
                    _skillTImelineInfos.Add(new SkillTimelineInfo
                    {
                        SkillName = playableAsset.name,
                        TimelineClipInfoList = timelinClipInfoList
                    });

                    trackAsset.muted = true;
                }
            }
        }

        void IAnimationController.Play(string clip, float? duration)
        {
            //TODO: 【ここから】各MemberのanimatorControllerをnoneにし、ゲームスタート時にLocomotion/Combat/Skillのアニメーションがmixerに接続されていることをvisualizerで確認
        }
    }
}
