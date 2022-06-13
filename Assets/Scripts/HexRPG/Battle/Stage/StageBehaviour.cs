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
        public static Vector3 GetPos(this IStageController stageController, Hex root, Vector2 dir, int rotationAngle)
        {
            return root.transform.position + Quaternion.AngleAxis(rotationAngle, Vector3.up) * (stageController.DirX * dir.x + stageController.DirZ * dir.y);
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

        /// <summary>
        /// LandedHexÇ©ÇÁangleÇÃï˚å¸Ç÷êiÇÒÇæéûÇ…ç≈Ç‡ãﬂÇ¢EnemyHex
        /// </summary>
        /// <param name="angle"></param>
        /// <returns></returns>
        public static Hex GetNearestEnemyHexFromAngle(this IStageController stageController, Hex root, int angle)
        {
            while (true)
            {
                root = stageController.GetHex(root, new Vector2(0, 1), angle);
                if (root == null) break;
                if (root.IsPlayerHex == false) break;
            }
            return root;
        }
    }
}
