using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Profiling;
using System.Linq;

public class GCallocTest : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        var list = new List<int>(5);
        Debug.Log(list.Count);

        var array = new int[5];
        Debug.Log(array.Length);
    }

    // Update is called once per frame
    void Update()
    {
        Profiler.BeginSample("AllocCheck: IEnumerable");
        var list1 = new List<int>() { 1, 2, 3, 4 };
        var enumerable =
            from num in list1
            select num * num;
        for(int i = 0; i < 1; i++)
        {
            foreach (var num in enumerable) { }
        }
        
        Profiler.EndSample();

        Profiler.BeginSample("AllocCheck: ToList");
        var list2 = new List<int>() { 1, 2, 3, 4 };
        var enumerable2 =
            from num in list2
            select num * num;
        var list3 = enumerable2.ToList();
        for (int i = 0; i < 1; i++)
        {
            foreach (var num in list3) { }
        }
        Profiler.EndSample();



        var testList = new List<int> { 1, 2 };
        var list4 = new List<int>(1);
        Profiler.BeginSample("AllocCheck: ReAssignList");
        //list4 = new List<int>(2);
        //list4 = new List<int>(4);
        list4 = testList;
        Profiler.EndSample();

        var testArray = new int[] { 1, 2 };
        var array = new int[1];
        Profiler.BeginSample("AllocCheck: ReAssignArray");
        //array = new int[2];
        //array = new int[4];
        array = testArray;
        Profiler.EndSample();



        Profiler.BeginSample("AllocCheck: ReAssignList1");
        var list6 = new List<int>(100);
        list6 = testList;
        Profiler.EndSample();

        Profiler.BeginSample("AllocCheck: ReAssignList2");
        List<int> list7;
        list7 = testList;
        Profiler.EndSample();



        var list8 = new List<int>() { 1 };
        Profiler.BeginSample("AllocCheck: ForReAssignList1");
        for(int i = 0; i < 100; i++)
        {
            var list = new List<int>() { 1 };
        }
        Profiler.EndSample();

        Profiler.BeginSample("AllocCheck: ForReAssignList2");
        for (int i = 0; i < 100; i++)
        {
            var list = list8;
        }
        Profiler.EndSample();



        var array2 = new int[100];
        List<int> list9 = new List<int>(100);
        var testArray2 = Enumerable.Range(0, 100).ToArray();
        Profiler.BeginSample("AllocCheck: ForArrayTest");
        for (int i = 0; i < 100; i++)
        {
            test1(out array2);
        }
        Profiler.EndSample();

        Profiler.BeginSample("AllocCheck: ForListTest");
        for (int i = 0; i < 100; i++)
        {
            list9.Clear();
            test2(in list9);
        }
        Profiler.EndSample();

        void test1(out int[] results)
        {
            results = testArray2.Select(num => num - 1).ToArray();
        }

        void test2(in List<int> results)
        {
            foreach(var num in testArray2)
            {
                results.Add(num);
            }
        }



        int num1 = 0;
        Profiler.BeginSample("AllocCheck: lamdaTest1");
        Enumerable.Range(0, 10).Select(_ => num1++);
        Profiler.EndSample();

        Profiler.BeginSample("AllocCheck: lamdaTest2");
        Enumerable.Range(0, 10000).Select(_ => num1++);
        Profiler.EndSample();
    }
}
