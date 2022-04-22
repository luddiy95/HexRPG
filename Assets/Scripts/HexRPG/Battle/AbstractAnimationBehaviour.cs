using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace HexRPG.Battle
{
    public class AbstractAnimationBehaviour : MonoBehaviour
    {
        public virtual void SetupDamaged()
        {

        }

        public virtual void OnInspectorGUI()
        {
            if (GUILayout.Button("SetupDamaged"))
            {
                SetupDamaged();
            }
        }
    }
}
