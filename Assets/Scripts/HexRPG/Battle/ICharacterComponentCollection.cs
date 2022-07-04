
namespace HexRPG.Battle
{
    public interface ICharacterComponentCollection : IBaseComponentCollection
    {
        IProfileSetting ProfileSetting { get; }
        IDieObservable DieObservable { get; }
        IHealth Health { get; }
    }
}
