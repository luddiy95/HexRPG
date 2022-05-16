
namespace HexRPG.Battle.HUD
{
    public interface IGauge
    {
        void Set(int amount);
        void Init(int maxAmount);
    }
}
