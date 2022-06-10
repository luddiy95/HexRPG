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
        int RotationAngle { get; set; }
        int DefaultRotation { get; set; }

        /// <summary>
        /// HUD��\��������Ƃ���Transform
        /// </summary>
        Transform DisplayTransform { get; }

        /// <summary>
        /// spawnObj�𐶐�����ۂɐe�ƂȂ�Transform
        /// </summary>
        Transform SpawnRootTransform(string spawnObj);
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
            get => _rotationAngle; //! DefaultRotation����Ƃ�����]
            set
            {
                _rotationAngle = value;
                _rotation = Quaternion.Euler(0, _defaultRotation + _rotationAngle, 0);
            }
        }
        int _rotationAngle = 0;

        int ITransformController.DefaultRotation { get => _defaultRotation; set => _defaultRotation = value; } //! CameraRotateUnit(60)�̔{��
        int _defaultRotation = 0;

        Transform ITransformController.SpawnRootTransform(string spawnObj)
        {
            var spawnRootTransform = _spawnRoots.ToList().FirstOrDefault(x => x.SpawnObj == spawnObj);
            return (spawnRootTransform != null && spawnRootTransform.Transform != null) ? spawnRootTransform.Transform : transform;
        }

#nullable enable

        [Header("Root�ƂȂ�Transform�Bnull �Ȃ炱�̃I�u�W�F�N�g�B")]
        [SerializeField] Transform? _rootTransform;

        [Header("������Transform�Bnull �Ȃ炱�̃I�u�W�F�N�g�B")]
        [SerializeField] Transform? _moveTransform;

        [Header("��]������Transform�Bnull �Ȃ炱�̃I�u�W�F�N�g�B")]
        [SerializeField] Transform? _rotateTransform;

        [Header("HUD��\��������Ƃ���Transform�Bnull �Ȃ炱�̃I�u�W�F�N�g�B")]
        [SerializeField] Transform? _displayTransform;

#nullable disable

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

        /// <summary>
        /// target�̕��������ۂɕK�v��World��]
        /// </summary>
        /// <param name="transformController"></param>
        /// <param name="targetPos"></param>
        /// <returns></returns>
        public static int GetLookRotationAngleY(this ITransformController transformController, Vector3 targetPos)
        {
            var relativePos = targetPos - transformController.Position;
            relativePos.y = 0;
            return (int)Quaternion.LookRotation(relativePos).eulerAngles.y;
            //euler = (euler + 30) / 60 * 60 - transformController.RotationAngle;
            //return MathUtility.GetIntegerEuler(euler);
        }
    }
}
