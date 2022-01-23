using System.Collections;
using System.Collections.Generic;

namespace HexRPG.Battle
{
    public interface IActionStateController : IFeature
    {
        void Execute(Command command);

        void ExecuteTransition(ActionStateType stateType);

        void AddState(ActionState state);

        void SetInitialState(ActionState state);
    }
}
