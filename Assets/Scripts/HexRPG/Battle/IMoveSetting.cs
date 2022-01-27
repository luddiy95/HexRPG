
namespace HexRPG.Battle
{
    public interface IMoveSetting : IFeature
    {
        float MoveSpeed { get; }
        float RotateSpeed { get; }
    }
}
