
namespace HexRPG.Battle
{
    public interface ICharacterComponentCollection
    {
        ITransformController TransformController { get; }
        IHealth Health { get; }
    }
}
