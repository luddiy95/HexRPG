using UnityEngine;

namespace HexRPG.Battle
{
    public interface IProfileSetting : IFeature
    {
        Sprite StatusIcon { get; }
        Sprite OptionIcon { get; }
    }
}
