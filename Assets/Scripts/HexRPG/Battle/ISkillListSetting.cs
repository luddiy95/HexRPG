using UnityEngine;

namespace HexRPG.Battle
{
    public interface ISkillListSetting : IFeature
    {
        GameObject[] SkillList { get; }
    }
}
