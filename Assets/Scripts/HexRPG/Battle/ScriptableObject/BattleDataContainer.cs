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
        [SerializeField] int _cameraRotateUnit; //TODO: ��������60�Ŋm��ŗǂ��񂶂�Ȃ��H(Skill�������60�قڊm��Ŏg���Ă���)
        public int CameraRotateUnit => _cameraRotateUnit;

        // UI
        [SerializeField] Sprite _skillBackgroundDefaultSprite;
        public Sprite SkillBackgroundDefaultSprite => _skillBackgroundDefaultSprite;
        [SerializeField] Sprite _skillBackgroundSelectedSprite;
        public Sprite SkillBackgroundSelectedSprite => _skillBackgroundSelectedSprite;

        // Hex
        [SerializeField] Material _hexPlayerLineMat;
        public Material HexPlayerLineMat => _hexPlayerLineMat;

        [SerializeField] Material _hexDefaultMat;
        public Material HexDefaultMat => _hexDefaultMat;
        [SerializeField] Material _hexAttackIndicatedMat;
        public Material HexAttackIndicatedMat => _hexAttackIndicatedMat;
    }
}
