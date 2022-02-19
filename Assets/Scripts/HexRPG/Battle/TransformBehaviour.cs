using UnityEngine;
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

        /// <summary>
        /// �����𐶐�����ۂɐe�ƂȂ�Transform
        /// </summary>
        Transform SpawnRootTransform { get; }
    }

    public class TransformBehaviour : MonoBehaviour, ITransformController
    {
        Transform ITransformController.RootTransform => _rootTransform;

        Transform ITransformController.MoveTransform => _moveTransform;
        Vector3 ITransformController.Position { get => _moveTransform.localPosition; set => _moveTransform.localPosition = value; }

        Transform ITransformController.RotateTransform => _rotateTransform;
        Quaternion ITransformController.Rotation { get => _rotateTransform.rotation; set => _rotateTransform.rotation = value; }

        Transform ITransformController.SpawnRootTransform => _spawnRootTransform;

        [Header("Root�ƂȂ�Transform�Bnull �Ȃ炱�̃I�u�W�F�N�g�B")]
        [SerializeField] Transform _rootTransform;

        [Header("������Transform�Bnull �Ȃ炱�̃I�u�W�F�N�g�B")]
        [SerializeField] Transform _moveTransform;

        [Header("��]������Transform�Bnull �Ȃ炱�̃I�u�W�F�N�g�B")]
        [SerializeField] Transform _rotateTransform;

        [Header("�����𐶐�����ۂɐe�ƂȂ�Transform�Bnull �Ȃ炱�̃I�u�W�F�N�g�B")]
        [SerializeField] Transform _spawnRootTransform;

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
            if (_rootTransform == null) _rootTransform = transform;
            if (_moveTransform == null) _moveTransform = transform;
            if (_rotateTransform == null) _rotateTransform = transform;
            if (_spawnRootTransform == null) _spawnRootTransform = transform;

            (this as ITransformController).SetRotation(0);
            if (_spawnRoot != null) _rootTransform.SetParent(_spawnRoot);
            (this as ITransformController).Position = _spawnPos;
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

        public static void SetRotation(this ITransformController transformController, float angle)
        {
            transformController.Rotation = Quaternion.Euler(0, 30 + angle, 0);
        }
    }
}
