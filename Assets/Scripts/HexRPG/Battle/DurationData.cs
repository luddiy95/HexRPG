using System.Collections.Generic;
using System;
using UnityEngine;

namespace HexRPG.Battle
{
    [CreateAssetMenu(fileName = "DurationData", menuName = "ScriptableObjects/DurationData")]
    public class DurationData : ScriptableObject
    {
        /// <summary>
        /// Idle, Move›› -> Move››‘JˆÚ‚ÌDuration
        /// </summary>
        [Serializable]
        public class LocomotionDurationData
        {
            public string clipBefore;
            public string clipAfter;
            public float duration;
        }
        public float defaultLocomotionDuration = 0.25f;
        public List<LocomotionDurationData> locomotionDurations = new List<LocomotionDurationData>();

        /// <summary>
        /// Move››, Damaged, Combat’†’f -> Idle‘JˆÚ‚ÌDuration
        /// ¦ Combat‚Ì’ÊíI—¹, SkillI—¹ -> Idle‘JˆÚ‚ÌDuration‚ÍTimelineClip‚©‚çæ“¾‚·‚é
        /// </summary>
        [Serializable]
        public class BackToIdleDurationData
        {
            public string clipBefore;
            public float duration;
        }
        public float defaultBackToIdleDuration = 0.25f;
        public List<BackToIdleDurationData> backToIdleDurations = new List<BackToIdleDurationData>();

        /// <summary>
        /// Idle, Move›› -> Combat, Skill‘JˆÚ‚ÌDuration
        /// </summary>
        public float combatStartDuration = 0.25f;
        public float skillStartDuration = 0.25f;

        /// <summary>
        /// ”CˆÓ‚ÌClip -> Damaged‘JˆÚ‚ÌDuration
        /// </summary>
        [Serializable]
        public class DamagedDurationData
        {
            public string clipBefore;
            public float duration;
        }
        public float defaultDamagedDuration;
        public List<DamagedDurationData> damagedDurations = new List<DamagedDurationData>();

        /// <summary>
        /// Damaged -> Idle‘JˆÚ‚ÌExitTime‚ÆDuration
        /// </summary>
        public float exitTimeToIdle;

        /// <summary>
        /// ”O‚Ì‚½‚ß
        /// </summary>
        public float defaultDuration = 0.25f;
    }
}
