using System.Collections.Generic;
using UnityEngine;
using System;

namespace HexRPG.Battle
{
    [CreateAssetMenu]
    public class BattleDataContainer : ScriptableObject
    {
        [SerializeField] BattleData _data;
        public BattleData Data => _data;
    }

    [Serializable]
    public class BattleData
    {
        // Camera
        [SerializeField] int _cameraRotateUnit; //TODO: もうこれ60で確定で良いんじゃない？(Skillあたりで60ほぼ確定で使っている)
        public int CameraRotateUnit => _cameraRotateUnit;

        // Score
        public List<ScoreInfo> ScoreInfoMap => _scoreInfoMap;
        [SerializeField] List<ScoreInfo> _scoreInfoMap = new List<ScoreInfo>();
        public int InitScore => _initScore;
        [SerializeField] int _initScore;
        public int ScoreMax => _scoreMax;
        [SerializeField] int _scoreMax;

        // UI
        [SerializeField] Sprite _skillBackgroundDefaultSprite;
        public Sprite SkillBackgroundDefaultSprite => _skillBackgroundDefaultSprite;
        [SerializeField] Sprite _skillBackgroundSelectedSprite;
        public Sprite SkillBackgroundSelectedSprite => _skillBackgroundSelectedSprite;

        // Hex
        [SerializeField] Material _hexPlayerLineMat;
        public Material HexPlayerLineMat => _hexPlayerLineMat;
        [SerializeField] Material _hexEnemyLineMat;
        public Material HexEnemyLineMat => _hexEnemyLineMat;

        [SerializeField] Material _hexDefaultMat;
        public Material HexDefaultMat => _hexDefaultMat;
        [SerializeField] Material _hexAttackIndicatedMat;
        public Material HexAttackIndicatedMat => _hexAttackIndicatedMat;
    }
}
