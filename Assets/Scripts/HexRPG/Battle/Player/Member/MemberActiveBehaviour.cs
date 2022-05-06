using System;
using UniRx;

namespace HexRPG.Battle.Player.Member
{
    public interface IMemberSelectedObservable
    {
        IReadOnlyReactiveProperty<bool> IsSelected { get; }
    }

    public class MemberActiveBehaviour : ActiveBehaviour, IMemberSelectedObservable
    {
        IReadOnlyReactiveProperty<bool> IMemberSelectedObservable.IsSelected => _isSelected;
        readonly IReactiveProperty<bool> _isSelected = new ReactiveProperty<bool>(false);

        protected override void SetActive(bool visible)
        {
            base.SetActive(visible);
            _isSelected.Value = visible;
        }
    }
}
