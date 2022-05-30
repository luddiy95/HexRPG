using System.Collections.Generic;
using UnityEngine;
using System;

namespace HexRPG.Battle
{
    [CreateAssetMenu(fileName = "BattleData", menuName = "ScriptableObjects/BattleData")]
    public class BattleData : ScriptableObject
    {
        // Camera
        public int cameraRotateUnit;

        // Score
        public List<ScoreInfo> scoreInfoMap = new List<ScoreInfo>();
        public int initScore;
        public int scoreMax;

        // DamagedDisplay
        [Serializable]
        public class DamagedDisplayMatData
        {
            public HitType hitType;
            public Material material;
        }
        public List<DamagedDisplayMatData> damagedDisplayMatMap = new List<DamagedDisplayMatData>();

        // UI
        public Sprite skillBackgroundDefaultSprite;
        public Sprite skillBackgroundSelectedSprite;

        // Hex
        public Material hexPlayerLineMat;
        public Material hexEnemyLineMat;

        public Material hexDefaultMat;
        public Material hexAttackIndicatedMat;
    }
}
