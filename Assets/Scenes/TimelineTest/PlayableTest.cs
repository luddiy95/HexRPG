using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Assertions;
using UnityEngine.Playables;
using UniRx;
using System;
using System.Linq;

public class PlayableTest : MonoBehaviour
{
    Animator _animator;
    [SerializeField] AnimationClip[] _clips;

    PlayableGraph _graph;
    List<AnimationClipPlayable> _playables;
    AnimationMixerPlayable _mixer;

    [SerializeField, Range(0, 1)] float _weight = 0;

    private void Start()
    {
        _animator = GetComponent<Animator>();

        _graph = PlayableGraph.Create();
        AnimationPlayableOutput output = AnimationPlayableOutput.Create(_graph, "AnimationPlayer", _animator);

        _playables = _clips.Select(clip => AnimationClipPlayable.Create(_graph, clip)).ToList();
        _mixer = AnimationMixerPlayable.Create(_graph, _playables.Count, true);
        for (int i = 0; i < _playables.Count; i++)
        {
            _mixer.ConnectInput(i, _playables[i], 0);
        }

        output.SetSourcePlayable(_mixer);

        Disposable.Create(() => _graph.Destroy()).AddTo(this);

        _graph.Play();
    }

    private void Update()
    {
        _mixer.SetInputWeight(0, _weight);
        _mixer.SetInputWeight(1, 1 - _weight);
    }
}
