
namespace HexRPG.Battle
{
    public interface ICharacterComponentCollection
    {
        IColliderController ColliderController { get; }
        ITransformController TransformController { get; }
        IHealth Health { get; }
    }
}
