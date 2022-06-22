using System.Collections.Generic;
using UnityEngine;
using System.Linq;

namespace HexRPG.Battle.Stage
{
    public interface IStageController
    {
        Vector3 DirX { get; }
        Vector3 DirZ { get; }
    }

    public class StageBehaviour : MonoBehaviour, IStageController
    {
        readonly static float _diffX = Mathf.Sqrt(3) / 2f;
        readonly static float _diffZ = 3f / 2f;

        Vector3 IStageController.DirX => _dirX;
        Vector3 IStageController.DirZ => _dirZ;
        public readonly Vector3 _dirX = new Vector3(_diffX, 0, 0);
        public readonly Vector3 _dirZ = new Vector3(0, 0, _diffZ);

        [SerializeField] Transform _HexRoot;
        [SerializeField] Hex _hexPrefab;

        void Start()
        {
            //InitStage(9);
        }

        #region View

        void InitStage(int stageRadius)
        {
            for (int rad = 0; rad <= stageRadius; rad++)
            {
                foreach (Vector3 pos in (this as IStageController).GetAroundList(Vector3.zero, rad))
                {
                    Hex hex = Instantiate(_hexPrefab, _HexRoot);
                    hex.transform.position = pos;
                }
            }
        }

        #endregion
    }

    public static class StageExtensions
    {
        public static Vector3 GetPos(this IStageController stageController, Hex root, Vector2 dir, int rotationAngle)
        {
            return GetPos(stageController, root.transform.position, dir, rotationAngle);
        }

        static Vector3 GetPos(this IStageController stageController, Vector3 rootPos, Vector2 dir, int rotationAngle)
        {
            return rootPos + Quaternion.AngleAxis(rotationAngle, Vector3.up) * (stageController.DirX * dir.x + stageController.DirZ * dir.y);
        }

        public static Hex GetHex(this IStageController stageController, Hex root, Vector2 dir, int rotationAngle)
        {
            var position = stageController.GetPos(root, dir, rotationAngle);
            return TransformExtensions.GetLandedHex(position);
        }

        public static Hex[] GetHexList(this IStageController stageController, Hex root, List<Vector2> range, int rotationAngle)
        {
            return range
                .Select(dir => stageController.GetHex(root, dir, rotationAngle))
                .Where(rangeHex => rangeHex != null).ToArray();
        }

        public static List<Vector3> GetAroundList(this IStageController stageController, Vector3 rootPos, int rad)
        {
            var vertexList = new Vector3[] { rootPos };
            if(rad > 0)
            {
                vertexList = new Vector3[]
                {
                    stageController.GetPos(rootPos, new Vector2(-1, 1) * rad, 0),
                    stageController.GetPos(rootPos, new Vector2(-2, 0) * rad, 0),
                    stageController.GetPos(rootPos, new Vector2(-1, -1) * rad, 0),
                    stageController.GetPos(rootPos, new Vector2(1, -1) * rad, 0),
                    stageController.GetPos(rootPos, new Vector2(2, 0) * rad, 0),
                    stageController.GetPos(rootPos, new Vector2(1, 1) * rad, 0),
                };
            }

            List<Vector3> aroundList = new List<Vector3>(vertexList);
            for (int vt = 0; vt < vertexList.Length; vt++)
            {
                var diffVec = (vertexList[(vt + 1) % vertexList.Length] - vertexList[vt]) / rad;
                for (int i = 1; i <= rad - 1; i++)
                {
                    aroundList.Add(vertexList[vt] + diffVec * i);
                }
            }

            return aroundList;
        }

        public static Hex[] GetAroundHexList(this IStageController stageController, Hex root, int rad)
        {
            return stageController.GetAroundList(root.transform.position, rad)
                .Select(pos => TransformExtensions.GetLandedHex(pos))
                .Where(hex => hex != null)
                .ToArray();
        }
    }
}
