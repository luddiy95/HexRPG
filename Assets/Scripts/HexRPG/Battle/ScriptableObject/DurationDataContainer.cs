using System.Collections.Generic;
using System;
using UnityEngine;

namespace HexRPG.Battle
{
    [CreateAssetMenu(fileName = "DurationDataContainer", menuName = "ScriptableObjects/DurationDataContainer")]
    public class DurationDataContainer : ScriptableObject
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

        [Serializable]
        public class ClipDurationData
        {
            public string clip;
            public float duration;
        }

        /// <summary>
        /// Idle(¡‚Ì‚Æ‚±‚ëIdle‚Ì‚İ) -> Rotate››‘JˆÚ‚ÌDuration
        /// </summary>
        public float defaultRotateStartDuration = 0.25f;
        public List<ClipDurationData> rotateStartDurations = new List<ClipDurationData>();

        /// <summary>
        /// Move››, Damaged, Combat’†’f -> Idle‘JˆÚ‚ÌDuration
        /// ¦ Combat‚Ì’ÊíI—¹, SkillI—¹ -> Idle‘JˆÚ‚ÌDuration‚ÍTimelineClip‚©‚çæ“¾‚·‚é
        /// </summary>
        public float defaultBackToIdleDuration = 0.25f;
        public List<ClipDurationData> backToIdleDurations = new List<ClipDurationData>();

        /// <summary>
        /// Idle, Move›› -> Combat, Skill‘JˆÚ‚ÌDuration
        /// </summary>
        public float defaultCombatStartDuration = 0.25f;
        public List<ClipDurationData> combatStartDurations = new List<ClipDurationData>();
        public float defaultSkillStartDuration = 0.25f;
        public List<ClipDurationData> skillStartDurations = new List<ClipDurationData>();

        /// <summary>
        /// ”CˆÓ‚ÌClip -> Damaged‘JˆÚ‚ÌDuration
        /// </summary>
        public float defaultDamagedDuration = 0.25f;
        public List<ClipDurationData> damagedDurations = new List<ClipDurationData>();

        /// <summary>
        /// Damaged -> Idle‘JˆÚ‚ÌExitTime‚ÆDuration
        /// </summary>
        public float exitTimeToIdle;

        public float dieStartDuration = 0.25f;

        /// <summary>
        /// ”O‚Ì‚½‚ß
        /// </summary>
        public float defaultDuration = 0.25f;
    }
}
