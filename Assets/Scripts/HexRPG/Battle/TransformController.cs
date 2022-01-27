using UnityEngine;

namespace HexRPG.Battle
{
    using Stage;

    public interface ITransformController : IFeature
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

        /// <summary>
        /// 何かを生成する際に親となるTransform
        /// </summary>
        Transform SpawnRootTransform { get; }
    }

    public class TransformController : AbstractCustomComponentBehaviour, ITransformController
    {
        Transform ITransformController.RootTransform => _rootTransform;

        Transform ITransformController.MoveTransform => _moveTransform;
        Vector3 ITransformController.Position { get => _moveTransform.position; set => _moveTransform.position = value; }

        Transform ITransformController.RotateTransform => _rotateTransform;
        Quaternion ITransformController.Rotation { get => _rotateTransform.rotation; set => _rotateTransform.rotation = value; }

        Transform ITransformController.SpawnRootTransform => _spawnRootTransform;

        [Header("RootとなるTransform。null ならこのオブジェクト。")]
        [SerializeField] Transform _rootTransform;

        [Header("動かすTransform。null ならこのオブジェクト。")]
        [SerializeField] Transform _moveTransform;

        [Header("回転させるTransform。null ならこのオブジェクト。")]
        [SerializeField] Transform _rotateTransform;

        [Header("何かを生成する際に親となるTransform。null ならこのオブジェクト。")]
        [SerializeField] Transform _spawnRootTransform;

        public override void Register(ICustomComponentCollection owner)
        {
            base.Register(owner);

            owner.RegisterInterface<ITransformController>(this);

            if (_rootTransform == null) _rootTransform = transform;
            if (_moveTransform == null) _moveTransform = transform;
            if (_rotateTransform == null) _rotateTransform = transform;
            if (_spawnRootTransform == null) _spawnRootTransform = transform;
        }

        public override void Initialize()
        {
            base.Initialize();
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
