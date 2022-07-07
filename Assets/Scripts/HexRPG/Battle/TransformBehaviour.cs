using System.Linq;
using UnityEngine;
using System;

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
            var spawnRootTransform = _spawnRoots.FirstOrDefault(x => x.SpawnObj == spawnObj);
            return (spawnRootTransform != null && spawnRootTransform.Transform != null) ? spawnRootTransform.Transform : transform;
        }

        [Header("Root�ƂȂ�Transform�Bnull �Ȃ炱�̃I�u�W�F�N�g�B")]
        [SerializeField] Transform _rootTransform;

        [Header("������Transform�Bnull �Ȃ炱�̃I�u�W�F�N�g�B")]
        [SerializeField] Transform _moveTransform;

        [Header("��]������Transform�Bnull �Ȃ炱�̃I�u�W�F�N�g�B")]
        [SerializeField] Transform _rotateTransform;

        [Header("HUD��\��������Ƃ���Transform�Bnull �Ȃ炱�̃I�u�W�F�N�g�B")]
        [SerializeField] Transform _displayTransform;

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

        void Start()
        {
            //TODO: ���ꂢ��H
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

        static RaycastHit[] _hits = new RaycastHit[128];

        readonly static LayerMask hexLayerMask = 
            (1 << LayerMask.NameToLayer(PlayerHex) | 1 << LayerMask.NameToLayer(EnemyHex) | 1 << LayerMask.NameToLayer(NeutralHex));

        public static Hex GetLandedHex(this ITransformController transformController) => 
            GetHitHex(new Ray(transformController.Position + Vector3.up * 0.15f, Vector3.down), 0.3f);

        public static Hex GetLandedHex(Vector3 pos) =>
            GetHitHex(new Ray(pos + Vector3.up * 0.15f, Vector3.down), 0.3f);

        public static Hex[] GetSurroundedHexList(Hex root, float radius)
        {
            Physics.SphereCastNonAlloc(new Ray(root.transform.position, Vector3.down), radius, results: _hits, layerMask: hexLayerMask, maxDistance: 0);
            return _hits
                .Select(hit => hit.collider?.GetComponent<Hex>())
                .Where(hex => hex != null)
                .Where(hex => hex.GetDistanceXZ(root) <= radius) //! hex�́u���S�v��root�̒��S�ɑ΂��ċ���radius�ȉ�
                .ToArray();
        }

        static Hex GetHitHex(Ray ray, float maxDistance = Mathf.Infinity)
        {
            Physics.Raycast(ray, out var hit, maxDistance, hexLayerMask);
            return hit.collider?.GetComponent<Hex>();
        }

        /// <summary>
        /// target�̕��������ۂɕK�v��World��]
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
