using System.Collections.Generic;
using UnityEngine;

namespace HexRPG
{
    public class ObjectPool : MonoBehaviour
    {
        [SerializeField] GameObject _prefab;
        [Header("èâä˙ç≈ëÂêî")]
        [SerializeField] int _maxCount = 5;

        List<GameObject> _objList = new List<GameObject>();

        int _objSearchIndex = 0;

        void Awake()
        {
            for (int i = 0; i < _maxCount; i++) IncreaseObj();
        }

        public GameObject Instantiate()
        {
            var index = _objSearchIndex;
            for(int i = 0; i < _objList.Count; i++)
            {
                if (++index >= _objList.Count) index = 0; // indexÇ™_objListà»è„Ç…Ç»ÇÁÇ»Ç¢ÇÊÇ§Ç…

                var obj = _objList[index];

                if(obj.activeSelf == false)
                {
                    obj.SetActive(true);
                    _objSearchIndex = index;
                    return obj;
                }
            }

            var newObj = IncreaseObj();
            newObj.SetActive(true);
            return newObj;
        }

        public void Free(GameObject obj)
        {
            obj.SetActive(false);
        }

        GameObject IncreaseObj()
        {
            var obj = Instantiate(_prefab, transform);
            obj.SetActive(false);
            _objList.Add(obj);
            return obj;
        }
    }
}
