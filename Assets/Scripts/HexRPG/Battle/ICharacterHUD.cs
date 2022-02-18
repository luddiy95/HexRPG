using UnityEngine;

namespace HexRPG.Battle
{
    public interface ICharacterHUD
    {
        void Bind(ICharacterComponentCollection character);
    }
}
