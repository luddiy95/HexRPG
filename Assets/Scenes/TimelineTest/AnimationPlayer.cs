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
    /// ������
    /// </summary>
    /// <param name="animator"></param>
    /// <param name="clips"></param>
    void Initialize(Animator animator, AnimationClip[] clips);

    /// <summary>
    /// �w�薼��clip�Đ�
    /// </summary>
    /// <param name="clipName">clip��</param>
    void Play(string clipName);

    /// <summary>
    /// �N���b�v���o�^����Ă��邩�ǂ���
    /// </summary>
    /// <param name="clipName"></param>
    /// <returns></returns>
    bool HasClip(string clipName);

    void SetTimeScale(float scale);

    /// <summary>
    /// ���Đ����̃A�j���[�V��������(normalized���ꂽ�l)
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
    /// �Đ��o�^���Ă���clip
    /// </summary>
    private List<AnimationClipPlayable> m_Playables;

    /// <summary>
    /// �Sclip��mixer
    /// </summary>
    private AnimationMixerPlayable m_Mixer;

    /// <summary>
    /// ���Đ�����AnimationClip inde
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
    /// ������
    /// </summary>
    /// <param name="animator">�Đ��^�[�Q�b�gAnimator</param>
    /// <param name="clips">�Đ��ł���悤�ɂ�����AnimationClip�Q</param>
    public void Initialize(Animator animator, AnimationClip[] clips)
    {
        m_Graph = PlayableGraph.Create();

        Assert.IsNotNull(animator);
        Assert.IsNotNull(clips);
        Assert.IsTrue(clips.All(x => x != null));

        // �S AnimaitonClip �p�� playable ��p�ӂ��A�S�� Mixer �łȂ��B
        m_Playables = clips.Select(x => AnimationClipPlayable.Create(m_Graph, x)).ToList();

        m_Mixer = AnimationMixerPlayable.Create(m_Graph, m_Playables.Count, normalizeWeights: true);
        for (int i = 0; i < m_Playables.Count; ++i)
        {
            m_Mixer.ConnectInput(i, m_Playables[i], 0);
        }

        var output = AnimationPlayableOutput.Create(m_Graph, "AnimationPlayer", animator);
        output.SetSourcePlayable(m_Mixer);

        // �p������ PlayableGraph �� Destroy����B
        Disposable.Create(() => m_Graph.Destroy()).AddTo(this);

        // PlayableGraph �����J�n
        m_Graph.Play();
    }

    /// <summary>
    /// �w�薼��animation clip ���Đ�
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
