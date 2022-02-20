using UnityEngine;

namespace HexRPG.Battle
{
    public interface ICharacterComponentCollection
    {
        ITransformController TransformController { get; }
    }
}
