using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Assertions;
using UnityEngine.Playables;
using System.Linq;
using UniRx;

public interface IAnimationPlayer
{
    /// <summary>
    /// 初期化
    /// </summary>
    /// <param name="animator"></param>
    /// <param name="clips"></param>
    void Initialize(Animator animator, AnimationClip[] clips);

    /// <summary>
    /// 指定名のclip再生
    /// </summary>
    /// <param name="clipName">clip名</param>
    void Play(string clipName);

    /// <summary>
    /// クリップが登録されているかどうか
    /// </summary>
    /// <param name="clipName"></param>
    /// <returns></returns>
    bool HasClip(string clipName);

    void SetTimeScale(float scale);

    /// <summary>
    /// 今再生中のアニメーション時間(normalizedされた値)
    /// </summary>
    /// <value></value>
    float PlayingTime { get; }
}

public class AnimationPlayer : MonoBehaviour, IAnimationPlayer
{
    [SerializeField] private Animator m_Animator;
    [SerializeField] private AnimationClip[] m_Clips;

    /// <summary>
    /// PlayableGraph
    /// </summary>
    private PlayableGraph m_Graph;

    /// <summary>
    /// 再生登録しているclip
    /// </summary>
    private List<AnimationClipPlayable> m_Playables;

    /// <summary>
    /// 全clipのmixer
    /// </summary>
    private AnimationMixerPlayable m_Mixer;

    /// <summary>
    /// 今再生中のAnimationClip inde
    /// </summary>
    private int m_PlayingIndex = -1;

    private void Start()
    {
        if (m_Animator != null)
        {
            Initialize(m_Animator, m_Clips);
        }
    }

    /// <summary>
    /// 初期化
    /// </summary>
    /// <param name="animator">再生ターゲットAnimator</param>
    /// <param name="clips">再生できるようにしたいAnimationClip群</param>
    public void Initialize(Animator animator, AnimationClip[] clips)
    {
        m_Graph = PlayableGraph.Create();

        Assert.IsNotNull(animator);
        Assert.IsNotNull(clips);
        Assert.IsTrue(clips.All(x => x != null));

        // 全 AnimaitonClip 用に playable を用意し、全部 Mixer でつなぐ。
        m_Playables = clips.Select(x => AnimationClipPlayable.Create(m_Graph, x)).ToList();

        m_Mixer = AnimationMixerPlayable.Create(m_Graph, m_Playables.Count, normalizeWeights: true);
        for (int i = 0; i < m_Playables.Count; ++i)
        {
            m_Mixer.ConnectInput(i, m_Playables[i], 0);
        }

        var output = AnimationPlayableOutput.Create(m_Graph, "AnimationPlayer", animator);
        output.SetSourcePlayable(m_Mixer);

        // 廃棄時に PlayableGraph も Destroyする。
        Disposable.Create(() => m_Graph.Destroy()).AddTo(this);

        // PlayableGraph 処理開始
        m_Graph.Play();
    }

    /// <summary>
    /// 指定名のanimation clip を再生
    /// </summary>
    /// <param name="clipName"></param>
    void IAnimationPlayer.Play(string clipName)
    {
        InternalPlay(clipName);
    }

    private void InternalPlay(string clipName)
    {
        var index = m_Playables.FindIndex(x => x.GetAnimationClip().name == clipName);
        if (m_PlayingIndex >= 0)
        {
            m_Mixer.SetInputWeight(m_PlayingIndex, 0f);
        }
        if (index >= 0)
        {
            m_Mixer.SetInputWeight(index, 1f);
            m_Mixer.SetTime(0);
            m_Playables[index].SetTime(0);
            m_PlayingIndex = index;
        }
    }

    bool IAnimationPlayer.HasClip(string clipName)
    {
        return m_Playables.Any(x => x.GetAnimationClip().name == clipName);
    }

    void IAnimationPlayer.SetTimeScale(float scale)
    {
        if (scale > 0f)
        {
            m_Graph.Play();
            m_Graph.GetRootPlayable(m_PlayingIndex).SetSpeed(scale);
        }
        else if (scale == 0f)
        {
            m_Graph.Stop();
        }
    }

    float IAnimationPlayer.PlayingTime
    {
        get
        {
            if (m_PlayingIndex >= 0)
            {
                var input = m_Mixer.GetInput(m_PlayingIndex);
                return input.IsValid() ? (float)input.GetTime() : 0f;
            }
            else
            {
                return 0f;
            }
        }
    }
}
