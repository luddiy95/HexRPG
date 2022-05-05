
namespace HexRPG.Battle
{
    public interface ICharacterActionStateController
    {
        void Init();
    }

    public interface IActionStateController
    {
        void Execute(Command command);

        void ExecuteTransition(ActionStateType stateType);

        void AddState(ActionState state);

        void SetInitialState(ActionState state);
    }
}
