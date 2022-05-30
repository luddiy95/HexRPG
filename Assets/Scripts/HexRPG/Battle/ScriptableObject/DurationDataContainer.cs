using System.Collections.Generic;
using System;
using UnityEngine;

namespace HexRPG.Battle
{
    [CreateAssetMenu(fileName = "DurationDataContainer", menuName = "ScriptableObjects/DurationDataContainer")]
    public class DurationDataContainer : ScriptableObject
    {
        /// <summary>
        /// Idle, Move���� -> Move�����J�ڂ�Duration
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
        /// Idle(���̂Ƃ���Idle�̂�) -> Rotate�����J�ڂ�Duration
        /// </summary>
        public float defaultRotateStartDuration = 0.25f;
        public List<ClipDurationData> rotateStartDurations = new List<ClipDurationData>();

        /// <summary>
        /// Move����, Damaged, Combat���f -> Idle�J�ڂ�Duration
        /// �� Combat�̒ʏ�I��, Skill�I�� -> Idle�J�ڂ�Duration��TimelineClip����擾����
        /// </summary>
        public float defaultBackToIdleDuration = 0.25f;
        public List<ClipDurationData> backToIdleDurations = new List<ClipDurationData>();

        /// <summary>
        /// Idle, Move���� -> Combat, Skill�J�ڂ�Duration
        /// </summary>
        public float defaultCombatStartDuration = 0.25f;
        public List<ClipDurationData> combatStartDurations = new List<ClipDurationData>();
        public float defaultSkillStartDuration = 0.25f;
        public List<ClipDurationData> skillStartDurations = new List<ClipDurationData>();

        /// <summary>
        /// �C�ӂ�Clip -> Damaged�J�ڂ�Duration
        /// </summary>
        public float defaultDamagedDuration = 0.25f;
        public List<ClipDurationData> damagedDurations = new List<ClipDurationData>();

        /// <summary>
        /// Damaged -> Idle�J�ڂ�ExitTime��Duration
        /// </summary>
        public float exitTimeToIdle;

        public float dieStartDuration = 0.25f;

        /// <summary>
        /// �O�̂���
        /// </summary>
        public float defaultDuration = 0.25f;
    }
}
