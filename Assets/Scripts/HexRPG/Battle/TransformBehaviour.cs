using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Profiling;
using System;

namespace HexRPG.Battle
{
    using Stage;

    public interface ITransformController
    {
        /// <summary>
        /// RootとなるTransform
        /// </summary>
        Transform RootTransform { get; }

        /// <summary>
        /// 動かすTransform
        /// </summary>
        Transform MoveTransform { get; }
        Vector3 Position { get; set; }

        /// <summary>
        /// 回転させるTransform
        /// </summary>
        Transform RotateTransform { get; }
        Quaternion Rotation { get; set; }
        int RotationAngle { get; set; }
        int DefaultRotation { get; set; }

        /// <summary>
        /// HUDを表示させるときのTransform
        /// </summary>
        Transform DisplayTransform { get; }

        /// <summary>
        /// spawnObjを生成する際に親となるTransform
        /// </summary>
        Transform SpawnRootTransform(string spawnObj);

        void Init(Transform spawnRoot, Vector3 spawnPos);
    }

    public class TransformBehaviour : MonoBehaviour, ITransformController
    {
        ITransformController Self => this;

        Transform ITransformController.RootTransform => _rootTransform ? _rootTransform : _rootTransform = transform;

        Transform ITransformController.MoveTransform => _moveTransform ? _moveTransform : _moveTransform = transform;
        Vector3 ITransformController.Position 
        { 
            get => Self.MoveTransform.localPosition; 
            set => Self.MoveTransform.localPosition = value; 
        }

        Transform ITransformController.RotateTransform => _rotateTransform ? _rotateTransform : _rotateTransform = transform;
        Quaternion ITransformController.Rotation { get => _rotation; set => _rotation = value; }
        Quaternion _rotation 
        { 
            get => Self.RotateTransform.localRotation; 
            set => Self.RotateTransform.localRotation = value; 
        }

        Transform ITransformController.DisplayTransform => _displayTransform ? _displayTransform! : _displayTransform = transform;

        int ITransformController.RotationAngle
        {
            get => _rotationAngle; //! DefaultRotationを基準とした回転
            set
            {
                _rotationAngle = value;
                _rotation = Quaternion.Euler(0, _defaultRotation + _rotationAngle, 0);
            }
        }
        int _rotationAngle = 0;

        int ITransformController.DefaultRotation { get => _defaultRotation; set => _defaultRotation = value; } //! CameraRotateUnit(60)の倍数
        int _defaultRotation = 0;

        Transform ITransformController.SpawnRootTransform(string spawnObj)
        {
            var spawnRootTransform = _spawnRoots.FirstOrDefault(x => x.SpawnObj == spawnObj);
            return (spawnRootTransform != null && spawnRootTransform.Transform != null) ? spawnRootTransform.Transform : transform;
        }

        [Header("RootとなるTransform。null ならこのオブジェクト。")]
        [SerializeField] Transform _rootTransform;

        [Header("動かすTransform、LandedHexを計算するTransform。null ならこのオブジェクト。")]
        [SerializeField] Transform _moveTransform;

        [Header("回転させるTransform。null ならこのオブジェクト。")]
        [SerializeField] Transform _rotateTransform;

        [Header("HUDを表示させるときのTransform。null ならこのオブジェクト。")]
        [SerializeField] Transform _displayTransform;

        [Header("何かを生成する際に親となるTransform。null ならこのオブジェクト。")]
        [SerializeField] SpawnRoot[] _spawnRoots;

        [Serializable]
        class SpawnRoot
        {
            public Transform Transform => _transform;
            public string SpawnObj => _spawnObj;

            [Header("nullならこのオブジェクト")]
            [SerializeField] Transform _transform;

            [Header("spawnするオブジェクト")]
            [SerializeField] string _spawnObj;
        }

        void Start()
        {
            //TODO: これいる？
            Self.RotationAngle = 0;
        }

        void ITransformController.Init(Transform spawnRoot, Vector3 spawnPos)
        {
            Self.RootTransform.SetParent(spawnRoot);
            Self.Position = spawnPos;
        }
    }

    public static class TransformExtensions
    {
        readonly public static string PlayerHex = "PlayerHex";
        readonly public static string EnemyHex = "EnemyHex";
        readonly public static string NeutralHex = "NeutralHex";

        static Collider[] _hitColliders = new Collider[16];
        static Collider[] _surroundedColliders = new Collider[128];

        readonly public static LayerMask hexLayerMask = 
            (1 << LayerMask.NameToLayer(PlayerHex) | 1 << LayerMask.NameToLayer(EnemyHex) | 1 << LayerMask.NameToLayer(NeutralHex));

        public static Hex GetLandedHex(this ITransformController transformController)
        {
            return GetHitHex(transformController.Position);
        }

        public static Hex GetLandedHex(Vector3 pos)
        {
            return GetHitHex(pos);
        }

        public static void GetSurroundedHexList(Hex root, float radius, ref List<Hex> surroundedHex)
        {
            surroundedHex.Clear();

            Array.Clear(_surroundedColliders, 0, _surroundedColliders.Length);
            Physics.OverlapSphereNonAlloc(root.transform.position, radius, _surroundedColliders, hexLayerMask);
            Hex hex = default;
            foreach (var collider in _surroundedColliders)
            {
                if (collider?.TryGetComponent(out hex) != true) continue;
                if (hex.GetDistanceXZ(root) <= radius) surroundedHex.Add(hex);
            }
        }

        static Hex GetHitHex(Vector3 pos)
        {
            Array.Clear(_hitColliders, 0, _hitColliders.Length);
            Physics.OverlapSphereNonAlloc(pos, 0.25f, _hitColliders, hexLayerMask);

            float minDistance = Mathf.Infinity;
            Hex hex = default;
            Hex result = null;
            foreach (var collider in _hitColliders)
            {
                if (collider?.TryGetComponent(out hex) != true) continue;
                var distance = pos.GetDistance2XZ(hex.transform.position);
                if (distance < minDistance)
                {
                    result = hex;
                    minDistance = distance;
                }
            }
            return result;
        }

        /// <summary>
        /// targetの方を向く際に必要なWorld回転
        /// </summary>
        /// <param name="transformController"></param>
        /// <param name="targetPos"></param>
        /// <returns></returns>
        public static int GetLookRotationAngleY(this ITransformController transformController, Vector3 targetPos)
        {
            return (int)Quaternion.LookRotation(transformController.Position.GetRelativePosXZ(targetPos)).eulerAngles.y;
            //euler = (euler + 30) / 60 * 60 - transformController.RotationAngle;
            //return MathUtility.GetIntegerEuler(euler);
        }
    }
}
