using System.Collections.Generic;
using UnityEngine;
using System;
using Kogane;

namespace HexRPG.Battle
{
    [CreateAssetMenu(fileName = "BattleData", menuName = "ScriptableObjects/BattleData")]
    public class BattleData : ScriptableObject
    {
        // Camera
        public int cameraRotateUnit;

        // Attribute
        [Serializable] public class AttributeIconValuePair : SerializableKeyValuePair<Attribute, Sprite> { }
        [Serializable] public class AttributeIconDictionary : SerializableDictionary<Attribute, Sprite, AttributeIconValuePair> { }
        public AttributeIconDictionary attributeIconMap = new AttributeIconDictionary();
        [Serializable] public class SkillAttributeMaterialValuePair : SerializableKeyValuePair<Attribute, Material> { }
        [Serializable] public class SkillAttributeMaterialDictionary : SerializableDictionary<Attribute, Material, SkillAttributeMaterialValuePair> { }
        public SkillAttributeMaterialDictionary skillAttributeMaterialMap = new SkillAttributeMaterialDictionary();

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
