using UnityEngine;

namespace HexRPG.Battle.Stage
{
    public class Hex : MonoBehaviour
    {
        public enum Status
        {
            PLAYER,
            ENEMY
        }

        [SerializeField]
        private Status _status;
        public bool IsPlayerHex => _status == Status.PLAYER;

        bool _isAttackIndicated = false;
        public bool IsAttackIndicated
        {
            get { return _isAttackIndicated; }
            set
            {
                _isAttackIndicated = value;
                var materials = _renderer.materials;
                if (_isAttackIndicated) materials[0] = BattlePreference.Instance.HexAttackIndicatedMat;
                else materials[0] = BattlePreference.Instance.HexDefaultMat;
                _renderer.materials = materials;
            }
        }

        MeshRenderer _renderer;

        void Awake()
        {
            _renderer = GetComponent<MeshRenderer>();
        }

        public void Liberate()
        {
            if (_status == Status.PLAYER) return;

            var materials = _renderer.materials;
            materials[1] = BattlePreference.Instance.HexPlayerLineMat;
            _renderer.materials = materials;

            _status = Status.PLAYER;
        }
    }
}
