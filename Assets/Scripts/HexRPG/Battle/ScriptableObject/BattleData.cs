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
        [Serializable] public class SkillAttributeMatValuePair : SerializableKeyValuePair<Attribute, Material> { }
        [Serializable] public class SkillAttributeMatDictionary : SerializableDictionary<Attribute, Material, SkillAttributeMatValuePair> { }
        public SkillAttributeMatDictionary skillAttributeMatMap = new SkillAttributeMatDictionary();

        // Score
        public List<ScoreInfo> scoreInfoMap = new List<ScoreInfo>();
        public int initScore;
        public int scoreMax;

        // DamagedDisplay
        [Serializable] public class DamagedDisplayMatValuePair : SerializableKeyValuePair<HitType, Material> { }
        [Serializable] public class DamagedDisplayMatDictionary : SerializableDictionary<HitType, Material, DamagedDisplayMatValuePair> { }
        public DamagedDisplayMatDictionary damagedDisplayMatMap = new DamagedDisplayMatDictionary();

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
