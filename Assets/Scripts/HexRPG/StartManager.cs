using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Linq;
using Zenject;

namespace HexRPG
{
    using Battle;

    public class StartManager : MonoBehaviour
    {
        BattleData _battleData;

        [SerializeField] Transform _timeListRoot1;
        [SerializeField] Transform _timeListRoot2;

        [Inject]
        public void Construct(
            BattleData battleData
        )
        {
            _battleData = battleData;
        }

        void Start()
        {
            var battleClearDataMap = _battleData.battleClearDataMap;
            var battle1TimeList = battleClearDataMap.Where(data => data.battleName == "Battle1").Select(data => data.time);
            var battle2TimeList = battleClearDataMap.Where(data => data.battleName == "Battle2").Select(data => data.time);
            ShowTimeList(_timeListRoot1, battle1TimeList);
            ShowTimeList(_timeListRoot2, battle2TimeList);
        }

        void ShowTimeList(Transform timeListRoot, IEnumerable<float> timeList)
        {
            var sortedTimeList = timeList.OrderBy(time => time).ToList();
            var showTimeCount = Mathf.Min(timeList.Count(), 3);
            for (int i = 0; i < 3; i++)
            {
                var child = timeListRoot.GetChild(i);
                if (i <= showTimeCount - 1)
                {
                    child.gameObject.SetActive(true);
                    child.GetChild(1).GetComponent<Text>().text = sortedTimeList[i].GetTime();
                }
                else
                {
                    child.gameObject.SetActive(false);
                }
            }
        }

        public void StartStage1()
        {
            SceneManager.LoadScene("Battle1");
        }

        public void StartStage2()
        {
            SceneManager.LoadScene("Battle2");
        }
    }
}
