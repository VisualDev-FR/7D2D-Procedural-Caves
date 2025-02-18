using System;
using System.Collections.Generic;
using System.Linq;

public class HashedPriorityQueue<T>
{
    // see https://github.com/FyiurAmron/PriorityQueue
    private readonly SortedDictionary<float, List<T>> _sortedDictionary;

    private HashSet<T> _items;

    private int _count;

    public HashedPriorityQueue()
    {
        _items = new HashSet<T>();
        _sortedDictionary = new SortedDictionary<float, List<T>>();
        _count = 0;
    }

    public void Enqueue(T item, float priority)
    {
        if (!_sortedDictionary.TryGetValue(priority, out List<T> queue))
        {
            queue = new List<T>();
            _sortedDictionary.Add(priority, queue);
        }
        _items.Add(item);
        queue.Add(item);
        _count++;
    }

    public T Dequeue()
    {
        if (_count == 0)
            throw new InvalidOperationException("The priority queue is empty.");

        var firstPair = _sortedDictionary.First();
        var queue = firstPair.Value;
        var item = queue[0];

        queue.RemoveAt(0);

        if (queue.Count == 0)
        {
            _sortedDictionary.Remove(firstPair.Key);
        }
        _count--;

        _items.Remove(item);

        return item;
    }

    public T Dequeue(Random rand)
    {
        if (_count == 0)
            throw new InvalidOperationException("The priority queue is empty.");

        var firstPair = _sortedDictionary.First();
        var queue = firstPair.Value;
        var index = rand.Next(queue.Count);
        var item = queue[index];

        queue.RemoveAt(index);

        if (queue.Count == 0)
        {
            _sortedDictionary.Remove(firstPair.Key);
        }
        _count--;

        _items.Remove(item);

        return item;
    }

    public bool Contains(T element)
    {
        return _items.Contains(element);
    }

    public int Count => _count;
}
