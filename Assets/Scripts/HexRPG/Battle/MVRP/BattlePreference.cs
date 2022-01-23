using UnityEngine;
using System.Linq;

namespace HexRPG.Battle
{
    using Stage;

    public class BattlePreference : SingletonMonoBehaviour<BattlePreference>
    {
        readonly string HEX = "Hex";

        public Hex GetAnyLandedHex(Vector3 landedPos) => GetHitHex(new Ray(landedPos + Vector3.up * 0.15f, Vector3.down), 0.3f);

        Hex GetHitHex(Ray ray, float maxDistance = Mathf.Infinity)
        {
            Physics.Raycast(ray, out var hit, maxDistance, 1 << LayerMask.NameToLayer(HEX));
#nullable enable
            return hit.collider?.GetComponent<Hex>();
#nullable disable
        }

        [SerializeField] Material _hexPlayerLineMat;
        public Material HexPlayerLineMat => _hexPlayerLineMat;

        [SerializeField] Material _hexDefaultMat;
        public Material HexDefaultMat => _hexDefaultMat;
        [SerializeField] Material _hexAttackIndicatedMat;
        public Material HexAttackIndicatedMat => _hexAttackIndicatedMat;
    }
}
