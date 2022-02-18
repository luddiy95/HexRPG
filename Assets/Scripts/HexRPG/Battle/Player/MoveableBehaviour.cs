using UnityEngine;

namespace HexRPG.Battle.Player
{
    using Stage;

    public interface IMoveableIndicator
    {
        void SwitchShow(bool isShow);
        void UpdateIndicator();
    }

    public class MoveableBehaviour : MonoBehaviour, IMoveableIndicator
    {
        [Header("移動できるHexを示すインジケータ")]
        [SerializeField] Transform _moveableIndicatorRoot;

        void IMoveableIndicator.SwitchShow(bool isShow)
        {
            _moveableIndicatorRoot.gameObject.SetActive(isShow);
        }

        void IMoveableIndicator.UpdateIndicator()
        {
            for (int i = 0; i < _moveableIndicatorRoot.childCount; i++)
            {
                Transform indicator = _moveableIndicatorRoot.GetChild(i);
                Hex landedHex = TransformExtensions.GetLandedHex(indicator.position);
                if (landedHex == null) indicator.gameObject.SetActive(false);
                else indicator.gameObject.SetActive(landedHex.IsPlayerHex);
            }

            _moveableIndicatorRoot.gameObject.SetActive(true);
        }
    }
}
