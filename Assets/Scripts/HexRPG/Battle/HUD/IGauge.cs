
namespace HexRPG.Battle.HUD
{
    public interface IGauge
    {
        int Amount { set; }
        void Init(int maxAmount);
    }
}
