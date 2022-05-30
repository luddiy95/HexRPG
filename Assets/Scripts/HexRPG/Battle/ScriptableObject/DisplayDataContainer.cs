using System.Collections.Generic;
using UnityEngine;
using System;

namespace HexRPG.Battle
{
    public enum DisplayType
    {
        GAUGE,
        DAMAGED_PANEL
    }

    [CreateAssetMenu(fileName = "DisplayDataContainer", menuName = "ScriptableObjects/DisplayDataContainer")]
    public class DisplayDataContainer : ScriptableObject
    {
        public List<DisplayData> displayDataMap = new List<DisplayData>();

        [Serializable]
        public class DisplayData
        {
            public string name;
            public Vector2 gaugeOffset;
            public Vector2 damagedPanelOffset;
            public Vector2 damagedPanelSize;
        }
    }
}
