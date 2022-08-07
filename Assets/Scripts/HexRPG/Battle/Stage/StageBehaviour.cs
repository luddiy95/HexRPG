using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using UnityEngine.Profiling;

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
            var arroundPosList = new List<Vector3>(128);

            for (int rad = 0; rad <= stageRadius; rad++)
            {
                (this as IStageController).GetArroundPosList(Vector3.zero, rad, ref arroundPosList);
                foreach (Vector3 pos in arroundPosList)
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
        static Vector2[] _vertexList = new Vector2[]
                {
                    new Vector2(-1, 1),
                    new Vector2(-2, 0),
                    new Vector2(-1, -1),
                    new Vector2(1, -1),
                    new Vector2(2, 0),
                    new Vector2(1, 1)
                };

        static List<Vector3> _arroundPosList = new List<Vector3>(128);

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

        public static void GetHexList(this IStageController stageController, Hex root, List<Vector2> range, int rotationAngle, ref List<Hex> hexList)
        {
            hexList.Clear();
            foreach (var dir in range)
            {
                var hex = stageController.GetHex(root, dir, rotationAngle);
                if (hex != null) hexList.Add(hex);
            }
        }

        public static void GetArroundPosList(this IStageController stageController, Vector3 rootPos, int rad, ref List<Vector3> arroundList)
        {
            var vertexList = new Vector3[] { rootPos };
            if(rad > 0)
            {
                vertexList = _vertexList.Select(vertex => stageController.GetPos(rootPos, vertex * rad, 0)).ToArray();
            }

            arroundList.Clear();
            foreach (var vertex in vertexList) arroundList.Add(vertex);
            for (int vt = 0; vt < vertexList.Length; vt++)
            {
                var diffVec = (vertexList[(vt + 1) % vertexList.Length] - vertexList[vt]) / rad;
                for (int i = 1; i <= rad - 1; i++)
                {
                    arroundList.Add(vertexList[vt] + diffVec * i);
                }
            }
        }

        public static void GetArroundHexList(this IStageController stageController, Hex root, int rad, ref List<Hex> arroundHexList)
        {
            arroundHexList.Clear();

            stageController.GetArroundPosList(root.transform.position, rad, ref _arroundPosList);
            foreach (var pos in _arroundPosList)
            {
                var hex = TransformExtensions.GetLandedHex(pos);
                if (hex != null) arroundHexList.Add(hex);
            }
        }
    }
}
