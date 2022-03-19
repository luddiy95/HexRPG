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
        /// Root�ƂȂ�Transform
        /// </summary>
        Transform RootTransform { get; }

        /// <summary>
        /// ������Transform
        /// </summary>
        Transform MoveTransform { get; }
        Vector3 Position { get; set; }

        /// <summary>
        /// ��]������Transform
        /// </summary>
        Transform RotateTransform { get; }
        Quaternion Rotation { get; set; }
        int RotationAngle { set; }
        int DefaultRotation { get; set; }

        /// <summary>
        /// spawnObj�𐶐�����ۂɐe�ƂȂ�Transform
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

        [Header("Root�ƂȂ�Transform�Bnull �Ȃ炱�̃I�u�W�F�N�g�B")]
        [SerializeField] Transform _rootTransform;

        [Header("������Transform�Bnull �Ȃ炱�̃I�u�W�F�N�g�B")]
        [SerializeField] Transform _moveTransform;

        [Header("��]������Transform�Bnull �Ȃ炱�̃I�u�W�F�N�g�B")]
        [SerializeField] Transform _rotateTransform;

        [Header("�����𐶐�����ۂɐe�ƂȂ�Transform�Bnull �Ȃ炱�̃I�u�W�F�N�g�B")]
        [SerializeField] SpawnRoot[] _spawnRoots;

        [Serializable]
        class SpawnRoot
        {
            public Transform Transform => _transform;
            public string SpawnObj => _spawnObj;

            [Header("null�Ȃ炱�̃I�u�W�F�N�g")]
            [SerializeField] Transform _transform;

            [Header("spawn����I�u�W�F�N�g")]
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
