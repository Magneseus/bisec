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
    public bool IsIndexable { get; private set; }

    public ActiveList()
    {
        rootNode = new ActiveNode<T>();
        rootNode.nextNode = rootNode;
        rootNode.nextActiveNode = rootNode;
        rootNode.prevActiveNode = rootNode;
        rootNode.prevNode = rootNode;
        rootNode.isRootNode = true;

        IsIndexable = true;
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
        Add(item, true);
    }

    public ActiveNode<T> Add(T item, bool active = true)
    {
        ActiveNode<T> newNode = new ActiveNode<T>(item);

        newNode.prevNode = rootNode.prevNode;
        newNode.nextNode = rootNode;

        newNode.prevNode.nextNode = newNode;

        rootNode.prevNode = newNode;

        Count++;

        if (active)
        {
            newNode.prevActiveNode = rootNode.prevActiveNode;
            newNode.nextActiveNode = rootNode;

            newNode.prevActiveNode.nextActiveNode = newNode;

            rootNode.prevActiveNode = newNode;

            newNode.activeIndex = ActiveCount++;
        }
        
        return newNode;
    }

    public ActiveNode<T> Insert(T item, int index, bool active = true)
    {
        ActiveNode<T> node = GetNodeAt(index);
        return InsertBefore(item, node, active);
    }

    public ActiveNode<T> InsertBefore(T itemToAdd, ActiveNode<T> beforeNode, bool active = true)
    {
        ActiveNode<T> newNode = new ActiveNode<T>(itemToAdd);

        newNode.prevNode = beforeNode.prevNode;
        newNode.nextNode = beforeNode;

        newNode.prevNode.nextNode = newNode;

        beforeNode.prevNode = newNode;

        Count++;

        if (active)
        {
            newNode.prevActiveNode = beforeNode.prevActiveNode;
            newNode.nextActiveNode = beforeNode;

            newNode.prevActiveNode.nextActiveNode = newNode;

            beforeNode.prevActiveNode = newNode;

            IsIndexable = false;
            ActiveCount++;
        }
        
        return newNode;
    }

    public bool Remove(T item)
    {
        ActiveNode<T> it = GetNodeAt(item);
        
        return Remove(it);
    }

    public bool Remove(ActiveNode<T> nodeToRemove)
    {
        if (nodeToRemove != null)
        {
            ActiveNode<T> pNode = nodeToRemove.prevNode;
            ActiveNode<T> nNode = nodeToRemove.nextNode;
            ActiveNode<T> paNode = nodeToRemove.prevActiveNode;
            ActiveNode<T> naNode = nodeToRemove.nextActiveNode;

            pNode.nextNode = nNode;
            nNode.prevNode = pNode;

            paNode.nextActiveNode = naNode;
            naNode.prevActiveNode = paNode;

            if (nodeToRemove.IsActive())
                ActiveCount--;
            Count--;
            nodeToRemove = null;

            IsIndexable = false;

            return true;
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
        IsIndexable = true;
    }

    public bool Contains(T item)
    {
        return GetNodeAt(item) != null;
    }

    public T Get(int index)
    {
        ActiveNode<T> node = GetNodeAt(index);

        return node.data;
    }

    public T Set(T item, int index)
    {
        ActiveNode<T> node = GetNodeAt(index);

        T returnVal = node.data;
        node.data = item;

        return returnVal;
    }

    public T this[int index]
    {
        get
        {
            return Get(index);
        }
        set
        {
            Set(value, index);
        }
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

    public void CopyActiveTo(T[] array, int arrayIndex)
    {
        if (array == null)
            throw new ArgumentNullException();
        else if (arrayIndex < 0 || arrayIndex >= array.Length)
            throw new ArgumentOutOfRangeException();
        else if (array.Length - arrayIndex > ActiveCount)
            throw new ArgumentException("The provided array does not contain enough elements.");

        ActiveNode<T> it = rootNode.nextActiveNode;
        int ind = arrayIndex;
        while (!it.isRootNode)
        {
            array[ind] = it.data;
            it.activeIndex = ind;

            it = it.nextActiveNode;
            ind++;
        }

        IsIndexable = true;
    }
    
    public void CopyActiveTo(List<T> array)
    {
        if (array == null)
            throw new ArgumentNullException();

        ActiveNode<T> it = rootNode.nextActiveNode;
        int ind = 0;
        while (!it.isRootNode)
        {
            array.Add(it.data);
            it.activeIndex = ind;

            it = it.nextActiveNode;
            ind++;
        }

        IsIndexable = true;
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

    public IEnumerator<T> GetActiveEnumerator()
    {
        ActiveNode<T> it = rootNode.nextActiveNode;
        int activeIndex = 0;
        while (!it.isRootNode)
        {
            it.activeIndex = activeIndex++;
            yield return it.data;

            it = it.nextActiveNode;
        }

        IsIndexable = true;
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

    public ActiveNode<T> GetNodeAt(int index)
    {
        ActiveNode<T> it = rootNode.nextNode;
        for (int i = 0; i < index; i++, it = it.nextNode) ;
        return it;
    }

    public ActiveNode<T> GetNodeAt(T item)
    {
        ActiveNode<T> it = rootNode.nextNode;

        while (!it.isRootNode)
        {
            if (it.data.Equals(item))
            {
                return it;
            }

            it = it.nextNode;
        }

        return null;
    }
    
    public ActiveNode<T> GetRootNode()
    {
        return rootNode;
    }
    
    public void GenerateActiveIndex()
    {
        ActiveNode<T> it = rootNode.nextActiveNode;
        int ind = 0;
        
        while (!it.isRootNode)
        {
            it.activeIndex = ind;
            
            it = it.nextActiveNode;
            ind++;
        }
        
        IsIndexable = true;
    }
}