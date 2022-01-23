using UnityEngine;

namespace HexRPG.Battle
{
    public class DataCenter : SingletonMonoBehaviour<DataCenter>
    {
        [SerializeField] Material _hexPlayerLineMat;
        public Material HexPlayerLineMat => _hexPlayerLineMat;

        [SerializeField] Material _hexDefaultMat;
        public Material HexDefaultMat => _hexDefaultMat;
        [SerializeField] Material _hexAttackIndicatedMat;
        public Material HexAttackIndicatedMat => _hexAttackIndicatedMat;
    }
}
