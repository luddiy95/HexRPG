using UnityEngine;

namespace HexRPG.Battle
{
    public interface IProfileSetting
    {
        string Name { get; }
        Sprite Icon { get; }

        Attribute Attribute { get; }
    }
}
