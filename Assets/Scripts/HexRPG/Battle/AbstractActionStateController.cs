using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace HexRPG.Battle
{
    public class AbstractActionStateController : MonoBehaviour
    {
        public virtual void Damaged()
        {

        }

        public virtual void OnInspectorGUI()
        {
            if (GUILayout.Button("Damaged"))
            {
                Damaged();
            }
        }
    }
}
