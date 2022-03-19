using System.Linq;
using UnityEngine;
using System;
using Zenject;

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
        int RotationAngle { set; }
        int DefaultRotation { get; set; }

        /// <summary>
        /// spawnObjを生成する際に親となるTransform
        /// </summary>
        Transform SpawnRootTransform(string spawnObj);
    }

    public class TransformBehaviour : MonoBehaviour, ITransformController
    {
        ITransformController Self => this;

        Transform ITransformController.RootTransform => _rootTransform != null ? _rootTransform : transform;

        Transform ITransformController.MoveTransform => _moveTransform != null ? _moveTransform : transform;
        Vector3 ITransformController.Position 
        { 
            get => Self.MoveTransform.localPosition; 
            set => Self.MoveTransform.localPosition = value; 
        }

        Transform ITransformController.RotateTransform => _rotateTransform != null ? _rotateTransform : transform;
        Quaternion ITransformController.Rotation { get => _rotation; set => _rotation = value; }
        Quaternion _rotation 
        { 
            get => Self.RotateTransform.rotation; 
            set => Self.RotateTransform.rotation = value; 
        }

        int ITransformController.RotationAngle { set => _rotation = Quaternion.Euler(0, _defaultRotation + value, 0); }

        int ITransformController.DefaultRotation { get => _defaultRotation; set => _defaultRotation = value; }
        int _defaultRotation = 0;

        Transform ITransformController.SpawnRootTransform(string spawnObj)
        {
            var spawnRootTransform = _spawnRoots.ToList().FirstOrDefault(x => x.SpawnObj == spawnObj);
            return (spawnRootTransform != null && spawnRootTransform.Transform != null) ? spawnRootTransform.Transform : transform;
        }

        [Header("RootとなるTransform。null ならこのオブジェクト。")]
        [SerializeField] Transform _rootTransform;

        [Header("動かすTransform。null ならこのオブジェクト。")]
        [SerializeField] Transform _moveTransform;

        [Header("回転させるTransform。null ならこのオブジェクト。")]
        [SerializeField] Transform _rotateTransform;

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

        Transform _spawnRoot;
        Vector3 _spawnPos;

        [Inject]
        public void Construct(
            Transform spawnRoot,
            Vector3 spawnPos
        )
        {
            _spawnRoot = spawnRoot;
            _spawnPos = spawnPos;
        }

        void Start()
        {
            if (_spawnRoot != null) Self.RootTransform.SetParent(_spawnRoot);
            Self.Position = _spawnPos;

            Self.RotationAngle = 0;
        }
    }

    public static class TransformExtensions
    {
        readonly static string HEX = "Hex";

        public static Hex GetLandedHex(this ITransformController transformController) => 
            GetHitHex(new Ray(transformController.Position + Vector3.up * 0.15f, Vector3.down), 0.3f);

        public static Hex GetLandedHex(Vector3 pos) =>
            GetHitHex(new Ray(pos + Vector3.up * 0.15f, Vector3.down), 0.3f);

        static Hex GetHitHex(Ray ray, float maxDistance = Mathf.Infinity)
        {
            Physics.Raycast(ray, out var hit, maxDistance, LayerMask.GetMask(HEX));
#nullable enable
            return hit.collider?.GetComponent<Hex>();
#nullable disable
        }
    }
}
