using UnityEngine;

namespace HexRPG.Battle
{
    using Stage;

    public interface ITransformController : IFeature
    {
        Transform Transform { get; }
        Vector3 Position { get; set; }
        Quaternion Rotation { get; set; }
    }

    public class TransformController : AbstractCustomComponentBehaviour, ITransformController
    {
        Vector3 ITransformController.Position { get => _transform.position; set => _transform.position = value; }

        Quaternion ITransformController.Rotation { get => _transform.rotation; set => _transform.rotation = value; }

        Transform ITransformController.Transform => _transform;

        [Header("動かすTransform。null ならこのオブジェクト。")]
        [SerializeField] Transform _transform;

        public override void Register(ICustomComponentCollection owner)
        {
            base.Register(owner);

            owner.RegisterInterface<ITransformController>(this);

            if (_transform == null) _transform = transform;
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
