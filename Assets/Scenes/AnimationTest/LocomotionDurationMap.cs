using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AnimationTest
{
    public enum LocomotionType
    {
        Idle,
        Movefwd,
        Moverightfwd,
        Moveright,
        Moverightbwd,
        Movebwd,
        Moveleftbwd,
        Moveleft,
        Moveleftfwd
    };

    [CreateAssetMenu(fileName = "LocomotionDurationMap", menuName = "ScriptableObjects/LocomotionDurationMap")]
    public class LocomotionDurationMap : ScriptableObject
    {
        [System.Serializable]
        public class DurationData
        {
            public LocomotionType clipBefore;
            public LocomotionType clipAfter;
            public float duration;
        }

        public float defaultDuration;
        public DurationData[] durations;
    }
}
