using Serilog;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace Frameset.Core.Utils;
/// <summary>
/// Support LRU evict Weak Dictionary
/// </summary>
/// <typeparam name="TKey"></typeparam>
/// <typeparam name="TValue"></typeparam>
public class WeakLruDictionary<TKey, TValue> : IDictionary<TKey, TValue>, IDisposable where TValue : class
{
    private readonly ConcurrentDictionary<TKey, WeakReference<TValue>> lruDict = [];
    private readonly ConcurrentDictionary<TKey, LinkedListNode<TKey>> nodeDict = [];
    private readonly LinkedList<TKey> _lruList = new();
    private SpinLock _lruSpinLock = new SpinLock(false);

    private bool _isDisposed = false;

    public int MaxCapacity
    {
        get;
        set;
    } = 100;
    public WeakLruDictionary()
    {

    }
    ~WeakLruDictionary()
    {
        Dispose();
    }
    public WeakLruDictionary(int maxCapacity = 100) : this()
    {
        MaxCapacity = maxCapacity;
    }

    public TValue this[TKey key]
    {

        get
        {
            CheckDisposed();
            if (TryGetValue(key, out TValue value))
            {
                return value;
            }
            else
            {
                return null;
            }
        }
        set
        {
            CheckDisposed();
            Add(key, value);
        }
    }

    public ICollection<TKey> Keys => this.nodeDict.Keys;

    public ICollection<TValue> Values => lruDict.Select(kvp => kvp.Value.TryGetTarget(out var n) ? n : null).Where(v => v != null).ToList()!;

    public int Count => this.nodeDict.Count;

    public bool IsReadOnly => false;

    public void Add(TKey key, TValue value)
    {
        CheckDisposed();
        if (nodeDict.TryGetValue(key, out LinkedListNode<TKey> listNode) && this.lruDict.TryGetValue(key, out WeakReference<TValue> refrence))
        {
            if (refrence.TryGetTarget(out TValue node))
            {
                node = null!;
                lruDict.TryRemove(key, out _);
                WeakReference<TValue> weakReference = new WeakReference<TValue>(value);
                lruDict[key] = weakReference;

                bool lockTaken = false;
                try
                {
                    _lruSpinLock.Enter(ref lockTaken);
                    _lruList.Remove(listNode);
                    _lruList.AddLast(listNode);
                }
                finally
                {
                    if (lockTaken) _lruSpinLock.Exit();
                }
            }
        }
        else
        {
            bool lockTaken = false;
            lruDict[key] = new WeakReference<TValue>(value);
            try
            {
                _lruSpinLock.Enter(ref lockTaken);
                EvictIfOverCapacity();
                LinkedListNode<TKey> linkedListNode = new LinkedListNode<TKey>(key);
                nodeDict[key] = linkedListNode;
                _lruList.AddLast(linkedListNode);
            }
            finally
            {
                if (lockTaken) _lruSpinLock.Exit();
            }

        }

    }

    public void Add(KeyValuePair<TKey, TValue> item) => Add(item.Key, item.Value);
    public bool TryAdd(TKey key, TValue value)
    {
        if (!this.lruDict.TryGetValue(key, out _))
        {
            Add(key, value);
            return true;
        }
        return false;
    }

    public void Clear()
    {
        CheckDisposed();
        bool lockTaken = false;
        try
        {
            _lruSpinLock.Enter(ref lockTaken);
            _lruList.Clear();
            nodeDict.Clear();
        }
        finally
        {
            if (lockTaken) _lruSpinLock.Exit();
        }
    }

    public bool Contains(KeyValuePair<TKey, TValue> item)
    {
        return TryGetValue(item.Key, out var value) && EqualityComparer<TValue>.Default.Equals(value, item.Value);
    }

    public bool ContainsKey(TKey key) => nodeDict.ContainsKey(key);

    public void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex)
    {
        this.ToList().CopyTo(array, arrayIndex);
    }

    public bool Remove(TKey key)
    {
        CheckDisposed();
        bool lockTaken = false;
        try
        {
            _lruSpinLock.Enter(ref lockTaken);
            if (nodeDict.TryRemove(key, out var linknode))
            {
                _lruList.Remove(linknode);
            }
            linknode = null;
        }
        finally
        {
            if (lockTaken) _lruSpinLock.Exit();
        }
        if (lruDict.TryRemove(key, out WeakReference<TValue> reference) && reference.TryGetTarget(out TValue node))
        {
            node = null!;
            return true;
        }

        return false;
    }

    public bool Remove(KeyValuePair<TKey, TValue> item)
    {
        return Remove(item.Key);
    }

    public bool TryGetValue(TKey key, out TValue value)
    {
        if (lruDict.TryGetValue(key, out var valueReference) && valueReference.TryGetTarget(out value))
        {
            bool lockTaken = false;
            try
            {
                _lruSpinLock.Enter(ref lockTaken);
                if (nodeDict.TryGetValue(key, out LinkedListNode<TKey> linkNode))
                {
                    _lruList.Remove(linkNode);
                    _lruList.AddLast(linkNode);
                }
            }
            finally
            {
                if (lockTaken)
                {
                    _lruSpinLock.Exit();
                }
            }
            return true;
        }
        value = default;
        return false;
    }

    public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator() => nodeDict.Select(kvp => lruDict[kvp.Key].TryGetTarget(out var n) ? new KeyValuePair<TKey, TValue>(kvp.Key, n) : default).Where(p => p.Key != null).GetEnumerator();


    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();


    public void Dispose()
    {
        if (!_isDisposed)
        {
            bool lockTaken = false;
            try
            {
                _lruSpinLock.Enter(ref lockTaken);
                _lruList.Clear();
                nodeDict.Clear();
            }
            finally
            {
                if (lockTaken) _lruSpinLock.Exit();
            }
            lruDict.Clear();
            _isDisposed = true;
        }
    }
    private void CheckDisposed()
    {
        ObjectDisposedException.ThrowIf(_isDisposed, typeof(WeakLruDictionary<TKey, TValue>));
    }
    public void PurgeGarbageCollectedEntries()
    {
        CheckDisposed();
        // 遍历并发字典获取快照，避免多线程迭代冲突
        foreach (var kvp in lruDict)
        {
            // 如果弱引用指向的包装器节点已经彻底死掉（说明外界和内部均无强引用持有它）
            if (!kvp.Value.TryGetTarget(out _))
            {
                // 调用 Remove 彻底清除其在 nodeDict、_lruList 和 lruDict 中的残留
                Remove(kvp.Key);
            }
        }
    }

    private void EvictIfOverCapacity()
    {
        // 此处在自旋锁保护中
        while (_lruList.Count >= MaxCapacity)
        {
            var firstNode = _lruList.First;
            if (firstNode != null)
            {
                TKey evictedKey = firstNode.Value;
                Log.Debug("--evict key " + evictedKey.ToString());
                // 1. 从强引用链表和字典中移除
                _lruList.RemoveFirst();

                if (lruDict.TryRemove(evictedKey, out WeakReference<TValue> reference) && reference.TryGetTarget(out TValue removeNode))
                {
                    removeNode = null!;
                }
                nodeDict.TryRemove(evictedKey, out _);
            }
        }
    }
    internal class LRUNode
    {
        public TKey Key
        {
            get;
            internal set;
        }
        public TValue Value
        {
            get;
            internal set;
        }
        private LRUNode()
        {

        }

        public LRUNode(TKey key, TValue value)
        {
            Key = key;
            Value = value;
        }


    }
}
