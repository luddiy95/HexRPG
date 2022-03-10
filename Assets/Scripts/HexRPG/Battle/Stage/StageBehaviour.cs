using System.Collections.Generic;
using UnityEngine;
using System.Linq;

namespace HexRPG.Battle.Stage
{
    public interface IStageController
    {
        Vector3 DirX { get; }
        Vector3 DirZ { get; }

        void Liberate(List<Hex> hexList, bool isPlayer);
    }

    public class StageBehaviour : MonoBehaviour, IStageController
    {
        readonly static float _diffX = Mathf.Sqrt(3) / 2f;
        readonly static float _diffZ = 3f / 2f;

        Vector3 IStageController.DirX => _dirX;
        Vector3 IStageController.DirZ => _dirZ;
        public readonly Vector3 _dirX = new Vector3(_diffX * 2, 0, 0);
        public readonly Vector3 _dirZ = new Vector3(_diffX, 0, _diffZ);

        [SerializeField]
        Transform _HexRoot;
        [SerializeField]
        Hex _hexPrefab;
        [SerializeField]

        //TODO: IAttackController‚âIAttackReserve‚Æ“¯—l‚ÉILiberater‚ğì‚é‚×‚«
        void IStageController.Liberate(List<Hex> hexList, bool isPlayer)
        {
            if (isPlayer)
            {
                hexList.ForEach(hex => hex.Liberate());
            }
        }

        #region View

        void InitStage(int stageRadius)
        {
            List<Vector2> vertexList = new List<Vector2>();
            Vector3 hexPos = Vector3.zero;

            for (int rad = 0; rad <= stageRadius; rad++)
            {
                vertexList.Clear();

                vertexList.Add(new Vector2(-1 * _diffX * rad, _diffZ * rad));
                vertexList.Add(new Vector2(-1 * _diffX * rad * 2, 0));
                vertexList.Add(new Vector2(-1 * _diffX * rad, -1 * _diffZ * rad));
                vertexList.Add(new Vector2(_diffX * rad, -1 * _diffZ * rad));
                vertexList.Add(new Vector2(_diffX * rad * 2, 0));
                vertexList.Add(new Vector2(_diffX * rad, _diffZ * rad));

                foreach (Vector2 pos2 in vertexList)
                {
                    hexPos = new Vector3(pos2.x, 0, pos2.y);
                    Hex hex = Instantiate(_hexPrefab, _HexRoot);
                    hex.transform.position = hexPos;
                }
                Vector2 diffVec = Vector2.zero;
                List<Vector2> btwHexList = new List<Vector2>();
                for (int vt = 0; vt < 6; vt++)
                {
                    diffVec = (vertexList[(vt + 1) % 6] - vertexList[vt]) / rad;
                    for (int i = 1; i <= rad - 1; i++)
                    {
                        btwHexList.Add(vertexList[vt] + diffVec * i);
                    }
                }

                foreach (Vector2 pos2 in btwHexList)
                {
                    hexPos = new Vector3(pos2.x, 0, pos2.y);
                    Hex hex = Instantiate(_hexPrefab, _HexRoot);
                    hex.transform.position = hexPos;
                }
            }
        }

        #endregion
    }

    public static class StageExtensions
    {
        public static List<Hex> GetHexList(this IStageController stageController, Hex root, List<Vector2> range, int rotate)
        {
            return range
                .Select(dir =>
                {
                    Vector3 position = root.transform.position + 
                        Quaternion.AngleAxis(60 * rotate, Vector3.up) * (stageController.DirX * dir.x + stageController.DirZ * dir.y);
                    return TransformExtensions.GetLandedHex(position);
                })
                .Where(rangeHex => rangeHex != null).ToList();
        }
    }
}
