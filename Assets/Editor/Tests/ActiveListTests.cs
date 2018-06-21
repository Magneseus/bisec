using UnityEngine;
using UnityEngine.TestTools;
using NUnit.Framework;
using System.Collections;

public class ActiveListTests
{
    [Test]
    public void ActiveListEmptyConstructor()
    {
        ActiveList<int> list = new ActiveList<int>();

        Assert.IsTrue(list.Count == 0);
        Assert.IsTrue(list.ActiveCount == 0);
        Assert.IsTrue(list.IsIndexable);
    }

    [Test]
    public void ActiveListCopyConstructor()
    {
        ActiveList<int> list = new ActiveList<int>();

        list.Add(0);
        list.Add(1);
        list.Add(2);

        ActiveList<int> list2 = new ActiveList<int>(list);

        for (int i = 0; i < list.Count; i++)
        {
            Assert.IsTrue(list[i] == list2[i]);
        }

        list.Remove(0);

        Assert.IsFalse(list.Count == list2.Count);
    }
}
