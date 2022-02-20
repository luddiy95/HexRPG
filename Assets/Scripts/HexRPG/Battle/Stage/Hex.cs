using System.Collections.Generic;
using UnityEngine;
using Zenject;

namespace HexRPG.Battle.Stage
{
    public class Hex : MonoBehaviour
    {
        public enum Status
        {
            PLAYER,
            ENEMY
        }

        BattleData _battleData;

        [SerializeField]
        private Status _status;
        public bool IsPlayerHex => _status == Status.PLAYER;

        int _attackIndicateCount;
        int AttackIndicateCount
        {
            get { return _attackIndicateCount; }
            set
            {
                _attackIndicateCount = Mathf.Max(0, value);
                IsAttackIndicate = (_attackIndicateCount > 0);
            }
        }

        bool _isAttackIndicate = false;
        public bool IsAttackIndicate
        {
            get { return _isAttackIndicate; }
            private set
            {
                _isAttackIndicate = value;
                var materials = _renderer.materials;
                if (_isAttackIndicate) materials[0] = _battleData.HexAttackIndicatedMat;
                else materials[0] = _battleData.HexDefaultMat;
                _renderer.materials = materials;
            }
        }

        MeshRenderer _renderer;

        public List<IAttackApplicator> AttackApplicatorList => _attackApplicatorList;
        List<IAttackApplicator> _attackApplicatorList = new List<IAttackApplicator>();

        [Inject]
        public void Construct(BattleData battleData)
        {
            _battleData = battleData;
        }

        void Awake()
        {
            _renderer = GetComponent<MeshRenderer>();
        }

        public void SetAttackIndicate()
        {
            ++AttackIndicateCount;
        }

        public void ResetAttackIndicate()
        {
            --AttackIndicateCount;
        }

        public void AddAttackApplicator(IAttackApplicator attackApplicator)
        {
            _attackApplicatorList.Add(attackApplicator);
        }

        public void RemoveAttackApplicator(IAttackApplicator attackApplicator)
        {
            _attackApplicatorList.Remove(attackApplicator);
        }

        public void Liberate()
        {
            if (_status == Status.PLAYER) return;

            var materials = _renderer.materials;
            materials[1] = _battleData.HexPlayerLineMat;
            _renderer.materials = materials;

            _status = Status.PLAYER;
        }
    }
}
