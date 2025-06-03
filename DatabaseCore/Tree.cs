using System;
using log4net;
using System.Collections.Generic;

namespace DatabaseCore;

public class Tree<K, V> : IIndex<K, V>
{
    readonly ITreeNodeManager<K, V> nodeManager;
    readonly bool allowDuplicateKeys;

    public Tree(ITreeNodeManager<K, V> nodeManager, bool allowDuplicateKeys = false)
    {
        if (nodeManager == null)
        {
            throw new ArgumentNullException("nodeManager does not exist");
        }
        this.nodeManager = nodeManager;
        this.allowDuplicateKeys = allowDuplicateKeys;
    }

    public void Insert(K key, V value)
    {

    }

    public tuple<K, V> Get(K key)
    {

    }

    /// <summary>
    /// Delete specified entry
    /// </summary>
    public bool Delete(K key, V value, IComparer<V> valueComparer = null)
    {

    }

    /// <summary>
    /// Delete all entires of a specific key
    /// This can only be called when it is a unique tree || allowDuplicateKeys = false
    /// </summary>
    public bool Delete(K key)
    {
        if (allowDuplicateKeys == true)
        {
            throw new InvalidOperationException("This method should be called only from unique tree");
        }


    }
}