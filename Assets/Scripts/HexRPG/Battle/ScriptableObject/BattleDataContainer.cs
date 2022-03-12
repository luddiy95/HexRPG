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
        [SerializeField] Material _hexPlayerLineMat;
        public Material HexPlayerLineMat => _hexPlayerLineMat;

        [SerializeField] Material _hexDefaultMat;
        public Material HexDefaultMat => _hexDefaultMat;
        [SerializeField] Material _hexAttackIndicatedMat;
        public Material HexAttackIndicatedMat => _hexAttackIndicatedMat;

        [SerializeField] Material _mainBtnEnableMat;
        public Material MainBtnEnableMat => _mainBtnEnableMat;
        [SerializeField] Material _mainBtnDisableMat;
        public Material MainBtnDisableMat => _mainBtnDisableMat;

        [SerializeField] Material _subBtnEnableMat;
        public Material SubBtnEnableMat => _subBtnEnableMat;
        [SerializeField] Material _subBtnDisableMat;
        public Material SubBtnDisableMat => _subBtnDisableMat;
    }
}
