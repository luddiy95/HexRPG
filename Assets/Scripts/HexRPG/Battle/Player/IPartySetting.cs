using UnityEngine;

namespace HexRPG.Battle.Player
{
    //! �擾���@�Ɋւ�炸IPartySetting�o�R�Ńp�[�e�B���͎Q�Ƃł���
    public interface IPartySetting : IFeature
    {
        GameObject[] Party { get; }
    }
}
