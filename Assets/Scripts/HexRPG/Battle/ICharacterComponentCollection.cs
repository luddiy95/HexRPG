
namespace HexRPG.Battle
{
    public interface ICharacterComponentCollection
    {
        IDieObservable DieObservable { get; }
        ITransformController TransformController { get; }
        IHealth Health { get; }
    }
}
