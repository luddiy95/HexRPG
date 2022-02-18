using Zenject;

namespace HexRPG.Battle.Skill
{
    public class SkillInstaller : MonoInstaller
    {
        public override void InstallBindings()
        {
            Container.BindInterfacesTo<AttackController>().AsSingle();
        }
    }
}
