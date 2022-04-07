
namespace HexRPG.Battle
{
    public interface IAnimationController
    {
        void Init();
        void Play(string clip, float? duration = null);
    }

    public enum AnimationType
    {
        Locomotion,
        Damaged,
        Combat,
        Skill
    }
}
