using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ActiveList<T> : ICollection<T>
{
    private ActiveNode<T> rootNode;

    public int Count { get; private set; }
    public int ActiveCount { get; private set; }
    public bool IsReadOnly { get; private set; }

    public ActiveList()
    {
        rootNode = new ActiveNode<T>();
        rootNode.nextNode = rootNode;
        rootNode.nextActiveNode = rootNode;
        rootNode.prevActiveNode = rootNode;
        rootNode.prevNode = rootNode;
        rootNode.isRootNode = true;

        IsReadOnly = false;
        Count = 0;
        ActiveCount = 0;
    }

    public ActiveList(ICollection<T> copyList) : this()
    {
        foreach (T item in copyList)
        {
            Add(item);
        }
    }

    public void Add(T item)
    {
        ActiveNode<T> newNode = new ActiveNode<T>(item);

        newNode.prevNode = rootNode.prevNode;
        newNode.prevActiveNode = rootNode.prevActiveNode;
        newNode.nextNode = rootNode;
        newNode.nextActiveNode = rootNode;

        newNode.prevNode.nextNode = newNode;
        newNode.prevActiveNode.nextActiveNode = newNode;

        rootNode.prevNode = newNode;
        rootNode.prevActiveNode = newNode;

        Count++;
    }

    public bool Remove(T item)
    {
        ActiveNode<T> it = rootNode.nextNode;

        while (!it.isRootNode)
        {
            if (it.data.Equals(item))
            {
                ActiveNode<T> pNode = it.prevNode;
                ActiveNode<T> nNode = it.nextNode;
                ActiveNode<T> paNode = it.prevActiveNode;
                ActiveNode<T> naNode = it.nextActiveNode;

                pNode.nextNode = nNode;
                nNode.prevNode = pNode;

                paNode.nextActiveNode = naNode;
                naNode.prevActiveNode = paNode;

                it = null;
                Count--;

                return true;
            }

            it = it.nextNode;
        }

        return false;
    }

    public void Clear()
    {
        rootNode.nextNode = rootNode;
        rootNode.prevNode = rootNode;
        rootNode.nextActiveNode = rootNode;
        rootNode.prevActiveNode = rootNode;

        Count = 0;
        ActiveCount = 0;
    }

    public bool Contains(T item)
    {
        ActiveNode<T> it = rootNode.nextNode;

        while (!it.isRootNode)
        {
            if (it.data.Equals(item))
            {
                return true;
            }

            it = it.nextNode;
        }

        return false;
    }

    public void CopyTo(T[] array, int arrayIndex)
    {
        if (array == null)
            throw new ArgumentNullException();
        else if (arrayIndex < 0 || arrayIndex >= array.Length)
            throw new ArgumentOutOfRangeException();
        else if (array.Length - arrayIndex > Count)
            throw new ArgumentException("The provided array does not contain enough elements.");

        ActiveNode<T> it = rootNode.nextNode;
        int ind = arrayIndex;
        while (!it.isRootNode)
        {
            array[ind] = it.data;
            
            it = it.nextNode;
            ind++;
        }
    }

    public IEnumerator<T> GetEnumerator()
    {
        ActiveNode<T> it = rootNode.nextNode;
        while (!it.isRootNode)
        {
            yield return it.data;

            it = it.nextNode;
        }
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        ActiveNode<T> it = rootNode.nextNode;
        while (!it.isRootNode)
        {
            yield return it.data;

            it = it.nextNode;
        }
    }

    public static bool IsRoot(ActiveNode<T> node)
    {
        return node.isRootNode;
    }
}
