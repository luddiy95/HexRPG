using System.Collections.Generic;
using System;
using UnityEngine;

namespace HexRPG.Battle
{
    [CreateAssetMenu(fileName = "DurationData", menuName = "ScriptableObjects/DurationData")]
    public class DurationData : ScriptableObject
    {
        /// <summary>
        /// Idle, Move○○ -> Move○○遷移のDuration
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
        /// Idle(今のところIdleのみ) -> Rotate○○遷移のDuration
        /// </summary>
        public float defaultRotateStartDuration = 0.25f;
        public List<ClipDurationData> rotateStartDurations = new List<ClipDurationData>();

        /// <summary>
        /// Move○○, Damaged, Combat中断 -> Idle遷移のDuration
        /// ※ Combatの通常終了, Skill終了 -> Idle遷移のDurationはTimelineClipから取得する
        /// </summary>
        public float defaultBackToIdleDuration = 0.25f;
        public List<ClipDurationData> backToIdleDurations = new List<ClipDurationData>();

        /// <summary>
        /// Idle, Move○○ -> Combat, Skill遷移のDuration
        /// </summary>
        public float defaultCombatStartDuration = 0.25f;
        public List<ClipDurationData> combatStartDurations = new List<ClipDurationData>();
        public float defaultSkillStartDuration = 0.25f;
        public List<ClipDurationData> skillStartDurations = new List<ClipDurationData>();

        /// <summary>
        /// 任意のClip -> Damaged遷移のDuration
        /// </summary>
        public float defaultDamagedDuration = 0.25f;
        public List<ClipDurationData> damagedDurations = new List<ClipDurationData>();

        /// <summary>
        /// Damaged -> Idle遷移のExitTimeとDuration
        /// </summary>
        public float exitTimeToIdle;

        public float dieStartDuration = 0.25f;

        /// <summary>
        /// 念のため
        /// </summary>
        public float defaultDuration = 0.25f;
    }
}
