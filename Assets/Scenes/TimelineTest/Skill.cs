using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;
using Cinemachine;

public class Skill : MonoBehaviour
{
    [SerializeField] PlayableDirector _director;

    public void Bind(Animator animator, CinemachineBrain mainCamera, CinemachineVirtualCamera mainVirtualCamera)
    {
        foreach (var bind in _director.playableAsset.outputs)
        {
            if (bind.streamName == "Animation Track")
            {
                _director.SetGenericBinding(bind.sourceObject, animator);
            }

            if(bind.streamName == "Cinemachine Track")
            {
                _director.SetGenericBinding(bind.sourceObject, mainCamera);
            }
        }

        foreach(var trackAsset in (_director.playableAsset as TimelineAsset).GetOutputTracks())
        {
            if(trackAsset is CinemachineTrack cinemachineTrack)
            {
                foreach(var clip in cinemachineTrack.GetClips())
                {
                    if(clip.displayName == "Main CM vcam")
                    {
                        var cinemachineShot = clip.asset as CinemachineShot;
                        if (cinemachineShot != null)
                        {
                            _director.SetReferenceValue(cinemachineShot.VirtualCamera.exposedName, mainVirtualCamera);
                        }
                    }
                }
            }
        }
    }

    public void StartSkill()
    {
        _director.Play();
    }

    public void StartAttackEnable()
    {
        // çUåÇîÕàÕÇÃHexÇ…çUåÇîªíËÇïtó^
    }

    public void FinishAttackEnable()
    {
        // HexÇÃçUåÇîªíËÇñ≥å¯Ç…Ç∑ÇÈ
    }
}
