using System;

public class Heap<T> where T : IHeapItem<T>
{
    T[] items;
    int currentItemCount;

    public Heap(int maxHeapSize)
    {
        items = new T[maxHeapSize];
    }

    public void Add(T item)
    {
        item.heapIndex = currentItemCount;
        items[currentItemCount] = item;
        SortUp(item);
        currentItemCount++;
    }

    public T RemoveFirst()
    {
        T firstItem = items[0];
        currentItemCount--;
        items[0] = items[currentItemCount];
        items[0].heapIndex = 0;
        SortDown(items[0]);
        return firstItem;
    }

    public void UpdateItem(T item)
    {
        SortUp(item);
    }

    public int Count => currentItemCount;

    public bool Contains(T item)
    {
        if (item.heapIndex < 0 || item.heapIndex >= currentItemCount)
            return false;
        return Equals(items[item.heapIndex], item);
    }

    void SortDown(T item)
    {
        while (true)
        {
            int leftChildIndex = item.heapIndex * 2 + 1;
            int rightChildIndex = item.heapIndex * 2 + 2;
            int swapIndex = 0;

            if (leftChildIndex < currentItemCount)
            {
                swapIndex = leftChildIndex;
                if (rightChildIndex < currentItemCount)
                    if (items[leftChildIndex].CompareTo(items[rightChildIndex]) < 0)
                        swapIndex = rightChildIndex;

                if (item.CompareTo(items[swapIndex]) < 0)
                    Swap(item, items[swapIndex]);
                else return;
            }
            else return;
        }
    }

    void SortUp(T item)
    {
        while (item.heapIndex > 0)
        {
            int parentIndex = (item.heapIndex - 1) / 2;
            T parentItem = items[parentIndex];
            if (item.CompareTo(parentItem) > 0)
                Swap(item, parentItem);
            else break;
        }
    }

    void Swap(T itemA, T itemB)
    {
        items[itemA.heapIndex] = itemB;
        items[itemB.heapIndex] = itemA;
        int tempIndex = itemA.heapIndex;
        itemA.heapIndex = itemB.heapIndex;
        itemB.heapIndex = tempIndex;
    }
}

public interface IHeapItem<T> : IComparable<T>
{
    int heapIndex { get; set; }
}