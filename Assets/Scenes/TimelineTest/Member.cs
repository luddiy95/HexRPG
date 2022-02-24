using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cinemachine;

public class Member : MonoBehaviour
{
    Animator _animator;
    [SerializeField] Skill _skill;

    [SerializeField] CinemachineBrain _mainCamera;
    [SerializeField] CinemachineVirtualCamera _mainVirtualCamera;

    private void Start()
    {
        _animator = GetComponent<Animator>();

        _skill.Bind(_animator, _mainCamera, _mainVirtualCamera);
    }

    public void StartSkill()
    {
        _skill.StartSkill();
    }
}
