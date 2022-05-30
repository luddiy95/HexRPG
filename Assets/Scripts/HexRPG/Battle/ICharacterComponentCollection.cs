
namespace HexRPG.Battle
{
    public interface ICharacterComponentCollection
    {
        IProfileSetting ProfileSetting { get; }
        IDieObservable DieObservable { get; }
        ITransformController TransformController { get; }
        IHealth Health { get; }
        IDamageApplicable DamagedApplicable { get; }
    }
}
