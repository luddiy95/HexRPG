using System.Collections.Generic;
using System.Collections.ObjectModel;
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

        public ObservableCollection<IAttackReservation> AttackReservationList => _attackReservationList;
        readonly ObservableCollection<IAttackReservation> _attackReservationList = new ObservableCollection<IAttackReservation>();

        bool _isAttackIndicate = false;
        public bool IsAttackIndicate
        {
            get { return _isAttackIndicate; }
            private set
            {
                _isAttackIndicate = value;
                _materials[0] = _isAttackIndicate ? _battleData.hexAttackIndicatedMat : _battleData.hexDefaultMat;
                _renderer.materials = _materials;
            }
        }

        MeshRenderer _renderer;
        Material[] _materials;

        public List<IAttackApplicator> AttackApplicatorList => _attackApplicatorList;
        List<IAttackApplicator> _attackApplicatorList = new List<IAttackApplicator>();

        [Inject]
        public void Construct(BattleData battleData)
        {
            _battleData = battleData;

            _attackReservationList.CollectionChanged += (sender, e) =>
            {
                IsAttackIndicate = _attackReservationList.Count > 0;
            };
        }

        void Awake()
        {
            _renderer = GetComponent<MeshRenderer>();
            var materials = _renderer.materials;
            _materials = new Material[materials.Length];
            for(int i = 0; i < materials.Length; i++) _materials[i] = materials[i];
        }

        public void AddAttackReservation(IAttackReservation attackReservation)
        {
            _attackReservationList.Add(attackReservation);
        }

        public void RemoveAttackReservation(IAttackReservation attackReservation)
        {
            _attackReservationList.Remove(attackReservation);
        }

        public void AddAttackApplicator(IAttackApplicator attackApplicator)
        {
            _attackApplicatorList.Add(attackApplicator);
        }

        public void RemoveAttackApplicator(IAttackApplicator attackApplicator)
        {
            _attackApplicatorList.Remove(attackApplicator);
        }

        public bool Liberate(bool isPlayer)
        {
            if (isPlayer == (_status == Status.PLAYER)) return false;

            _materials[1] = isPlayer ? _battleData.hexPlayerLineMat : _battleData.hexEnemyLineMat;
            _renderer.materials = _materials;

            gameObject.layer = LayerMask.NameToLayer(TransformExtensions.PlayerHex);

            _status = isPlayer ? Status.PLAYER : Status.ENEMY;

            return true;
        }
    }
}
