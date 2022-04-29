using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UniRx;

public class ReactiveCollectionTest : MonoBehaviour
{
    public class Test
    {
        public int test;
        public Test(int test)
        {
            this.test = test;
        }
    }
    IReadOnlyReactiveCollection<Test> Collection => _collection;
    IReactiveCollection<Test> _collection = new ReactiveCollection<Test>();

    private void Start()
    {
        Collection.ObserveCountChanged()
            .Subscribe(count => Debug.Log(Collection.ToList().Count))
            .AddTo(this);

        _collection.Add(new Test(1));
        _collection.Add(new Test(2));
    }
}
