
namespace HexRPG.Battle
{
    using Stage;
    public interface IMoveController : IFeature
    {
        void StartMove(Hex destination);
    }
}
