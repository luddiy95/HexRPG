using System.Collections.Generic;
using System;
using UnityEngine;

namespace HexRPG.Battle
{
    [CreateAssetMenu(fileName = "DurationData", menuName = "ScriptableObjects/DurationData")]
    public class DurationData : ScriptableObject
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

        /// <summary>
        /// Move����, Damaged, Combat���f -> Idle�J�ڂ�Duration
        /// �� Combat�̒ʏ�I��, Skill�I�� -> Idle�J�ڂ�Duration��TimelineClip����擾����
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
        /// Idle, Move���� -> Combat, Skill�J�ڂ�Duration
        /// </summary>
        public float combatStartDuration = 0.25f;
        public float skillStartDuration = 0.25f;

        /// <summary>
        /// �C�ӂ�Clip -> Damaged�J�ڂ�Duration
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
        /// Damaged -> Idle�J�ڂ�ExitTime��Duration
        /// </summary>
        public float exitTimeToIdle;

        /// <summary>
        /// �O�̂���
        /// </summary>
        public float defaultDuration = 0.25f;
    }
}
