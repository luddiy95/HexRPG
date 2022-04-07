using UnityEngine;
using System;

namespace AnimationTest
{

    public enum AnimationType
    {
        Locomotion,
        Damaged,
        Combat,
        Skill
    }

    [CreateAssetMenu(fileName = "DurationData", menuName = "ScriptableObjects/DurationData")]
    public class DurationData : ScriptableObject
    {
        [Serializable]
        public class LocomotionDurationData
        {
            public string clipBefore;
            public string clipAfter;
            public float duration;
        }
        public float defaultLocomotionDuration;
        public LocomotionDurationData[] locomotionDurations;

        [Serializable]
        public class DamagedDurationData
        {
            public string clipBefore;
            public float duration;
        }
        public float defaultDamagedDuration;
        public DamagedDurationData[] damagedDurations;
        public float exitTimeToIdle;
        public float durationToIdle;
    }
}
