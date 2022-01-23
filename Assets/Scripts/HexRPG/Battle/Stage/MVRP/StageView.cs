using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace HexRPG.Battle.Stage
{
    public class StageView : MonoBehaviour
    {
        readonly static float _diffX = Mathf.Sqrt(3) / 2f;
        readonly static float _diffZ = 3f / 2f;

        public readonly Vector3 _dirX = new Vector3(_diffX * 2, 0, 0);
        public readonly Vector3 _dirZ = new Vector3(_diffX, 0, _diffZ);

        [SerializeField]
        Transform _HexRoot;
        [SerializeField]
        Hex _hexPrefab;
        [SerializeField]

        public void SetAttackIndicated(Hex hex)
        {
            //hex.IsAttackIndicated = true;
        }

        public void ResetAttackIndicatedHexList(List<Hex> attackIndicatedHexList)
        {
            //attackIndicatedHexList.ForEach(hex => hex.IsAttackIndicated = false);
        }

        public void LiberateHexList(List<Hex> liberateHexList)
        {
            liberateHexList.ForEach(hex => hex.Liberate());
        }

        public void InitStage(int stageRadius)
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
    }
}
