using UnityEngine;

namespace HexRPG.Battle.Player
{
    //! 取得方法に関わらずIPartySetting経由でパーティ情報は参照できる
    public interface IPartySetting : IFeature
    {
        GameObject[] Party { get; }
    }
}
